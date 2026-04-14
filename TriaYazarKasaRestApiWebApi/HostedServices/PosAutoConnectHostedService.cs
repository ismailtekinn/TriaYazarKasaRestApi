using System.Text.Json;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApiWebApi.HostedServices
{
    public class PosAutoConnectHostedService : BackgroundService
    {
        private static readonly TimeSpan BekoMonitorInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan BekoRetryDelay = TimeSpan.FromSeconds(3);
        private const string BekoToken = "TOKEN FINTECH";

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PosAutoConnectHostedService> _logger;

        public PosAutoConnectHostedService(
            IServiceProvider serviceProvider,
            ILogger<PosAutoConnectHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await EnsureHuginConnectedAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureBekoConnectedAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Beko baglanti izleme dongusu hatasi.");
                }

                try
                {
                    await Task.Delay(BekoMonitorInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task EnsureHuginConnectedAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var huginDeviceService = scope.ServiceProvider.GetRequiredService<IHuginDeviceService>();
            var autoConnectionStore = scope.ServiceProvider.GetRequiredService<IAutoConnectionStore>();

            try
            {
                var huginConnectResponse = await huginDeviceService.ConnectAsync(new HuginConnectRequestDto
                {
                    UseTcp = true,
                    IpAddress = "192.168.0.88",
                    Port = 4444,
                    SerialPort = "",
                    FiscalId = "FT40050248"
                });

                if (huginConnectResponse.IsConnected)
                {
                    autoConnectionStore.SetHugin(huginConnectResponse.ConnectionId);
                    _logger.LogInformation("Hugin otomatik baglandi. ConnectionId: {ConnectionId}", huginConnectResponse.ConnectionId);
                }
                else
                {
                    _logger.LogWarning("Hugin otomatik baglanamadi. Mesaj: {Message}", huginConnectResponse.Message);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hugin otomatik baglanti hatasi.");
            }
        }

        private async Task EnsureBekoConnectedAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var bekoDeviceService = scope.ServiceProvider.GetRequiredService<IBekoDeviceService>();
            var autoConnectionStore = scope.ServiceProvider.GetRequiredService<IAutoConnectionStore>();

            var connectionId = autoConnectionStore.BekoConnectionId;
            if (connectionId is null)
            {
                await TryConnectBekoAsync(bekoDeviceService, autoConnectionStore, cancellationToken);
                return;
            }

            var statusResponse = await bekoDeviceService.GetStatusAsync(connectionId.Value);
            if (statusResponse.Success && IsBekoConnected(statusResponse.Data))
                return;

            _logger.LogWarning(
                "Beko baglantisi kullanilamaz durumda. Mevcut ConnectionId: {ConnectionId}, Mesaj: {Message}",
                connectionId.Value,
                statusResponse.Message);

            await SafeDisconnectBekoAsync(bekoDeviceService, autoConnectionStore, connectionId.Value);
            await TryConnectBekoAsync(bekoDeviceService, autoConnectionStore, cancellationToken);
        }

        private async Task TryConnectBekoAsync(
            IBekoDeviceService bekoDeviceService,
            IAutoConnectionStore autoConnectionStore,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < 5 && !cancellationToken.IsCancellationRequested; i++)
            {
                var bekoConnectResponse = await bekoDeviceService.ConnectAsync(new BekoConnectRequestDto
                {
                    Token = BekoToken
                });

                if (bekoConnectResponse.IsConnected)
                {
                    autoConnectionStore.SetBeko(bekoConnectResponse.ConnectionId);
                    _logger.LogInformation("Beko otomatik baglandi. ConnectionId: {ConnectionId}", bekoConnectResponse.ConnectionId);
                    return;
                }

                _logger.LogWarning(
                    "Beko otomatik baglanti denemesi basarisiz. Deneme: {TryNo}, Mesaj: {Message}",
                    i + 1,
                    bekoConnectResponse.Message);

                await Task.Delay(BekoRetryDelay, cancellationToken);
            }
        }

        private async Task SafeDisconnectBekoAsync(
            IBekoDeviceService bekoDeviceService,
            IAutoConnectionStore autoConnectionStore,
            Guid connectionId)
        {
            try
            {
                await bekoDeviceService.DisconnectAsync(connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Beko baglantisi temizlenirken hata olustu. ConnectionId: {ConnectionId}", connectionId);
            }
            finally
            {
                if (autoConnectionStore.BekoConnectionId == connectionId)
                    autoConnectionStore.SetBeko(null);
            }
        }

        private static bool IsBekoConnected(object? data)
        {
            if (data is null)
                return false;

            try
            {
                using var document = JsonDocument.Parse(JsonSerializer.Serialize(data));
                if (!document.RootElement.TryGetProperty("isConnected", out var isConnectedElement))
                    return false;

                return isConnectedElement.ValueKind == JsonValueKind.True;
            }
            catch
            {
                return false;
            }
        }
    }
}
