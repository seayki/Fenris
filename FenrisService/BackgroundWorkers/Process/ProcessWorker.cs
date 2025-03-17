
using Fenris.DiscoveryServices;

namespace FenrisService.BackgroundWorkers.Process
{
    public class ProcessWorker : BackgroundService
    {
        private readonly ILogger<ProcessWorker> _logger;
        public ProcessWorker(ILogger<ProcessWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessTerminator.RunTerminateProcess();
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in worker loop");
                }
                await Task.Delay(3000, stoppingToken); 
            }
        }
    }
}
