using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApiWebApi.HostedServices
{
    public class PosAutoConnectHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PosAutoConnectHostedService> _logger;

        public PosAutoConnectHostedService(
            IServiceProvider serviceProvider,
            ILogger<PosAutoConnectHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var huginDeviceService = scope.ServiceProvider.GetRequiredService<IHuginDeviceService>();
            var bekoDeviceService = scope.ServiceProvider.GetRequiredService<IBekoDeviceService>();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hugin otomatik baglanti hatasi.");
            }

            try
            {
                var bekoConnectResponse = await bekoDeviceService.ConnectAsync(new BekoConnectRequestDto
                {
                    Token = "TOKEN FINTECH"
                });

                if (bekoConnectResponse.IsConnected)
                {
                    autoConnectionStore.SetBeko(bekoConnectResponse.ConnectionId);
                    _logger.LogInformation("Beko otomatik baglandi. ConnectionId: {ConnectionId}", bekoConnectResponse.ConnectionId);
                }
                else
                {
                    _logger.LogWarning("Beko otomatik baglanamadi. Mesaj: {Message}", bekoConnectResponse.Message);
                }

                for (int i = 0; i < 5; i++)
                {
                    bekoConnectResponse = await bekoDeviceService.ConnectAsync(new BekoConnectRequestDto
                    {
                        Token = "TOKEN FINTECH"
                    });

                    if (bekoConnectResponse.IsConnected)
                    {
                        autoConnectionStore.SetBeko(bekoConnectResponse.ConnectionId);
                        _logger.LogInformation("Beko otomatik baglandi. ConnectionId: {ConnectionId}", bekoConnectResponse.ConnectionId);
                        break;
                    }

                    _logger.LogWarning("Beko otomatik baglanti denemesi basarisiz. Deneme: {TryNo}, Mesaj: {Message}", i + 1, bekoConnectResponse.Message);

                    await Task.Delay(3000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beko otomatik baglanti hatasi.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}