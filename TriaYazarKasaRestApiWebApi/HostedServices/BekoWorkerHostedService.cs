using System.Diagnostics;
using System.Net.Http.Json;

namespace TriaYazarKasaRestApiWebApi.HostedServices
{
    public class BekoWorkerHostedService : BackgroundService
    {
        public const string WorkerUrl = "http://127.0.0.1:5107";
        private readonly ILogger<BekoWorkerHostedService> _logger;
        private Process? _workerProcess;

        public BekoWorkerHostedService(ILogger<BekoWorkerHostedService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_workerProcess == null || _workerProcess.HasExited)
                    {
                        StartWorkerProcess();
                        await WaitForHealthAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Beko worker baslatilirken veya izlenirken hata olustu.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_workerProcess is { HasExited: false })
                    _workerProcess.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Beko worker kapatilirken hata olustu.");
            }

            return base.StopAsync(cancellationToken);
        }

        private void StartWorkerProcess()
        {
            var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TriaYazarKasaRestApi.BekoWorker", "TriaYazarKasaRestApi.BekoWorker.csproj"));
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls {WorkerUrl}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(projectPath)!
            };

            _workerProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            _workerProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logger.LogInformation("BekoWorker: {Message}", args.Data);
            };
            _workerProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logger.LogWarning("BekoWorker: {Message}", args.Data);
            };

            _workerProcess.Start();
            _workerProcess.BeginOutputReadLine();
            _workerProcess.BeginErrorReadLine();
            _logger.LogInformation("Beko worker prosesi baslatildi. Pid: {Pid}", _workerProcess.Id);
        }

        private async Task WaitForHealthAsync(CancellationToken cancellationToken)
        {
            using var client = new HttpClient { BaseAddress = new Uri(WorkerUrl) };
            for (int i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var response = await client.GetFromJsonAsync<WorkerHealthResponse>("/health", cancellationToken);
                    if (response?.Status == "ok")
                    {
                        _logger.LogInformation("Beko worker saglik kontrolunu gecti.");
                        return;
                    }
                }
                catch
                {
                }

                await Task.Delay(500, cancellationToken);
            }

            throw new InvalidOperationException("Beko worker saglik kontrolune cevap vermedi.");
        }

        private sealed class WorkerHealthResponse
        {
            public string? Status { get; set; }
        }
    }
}
