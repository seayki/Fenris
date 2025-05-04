using Fenris;
using FenrisService.BackgroundWorkers.IconUrl;
using FenrisService.BackgroundWorkers.Process;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenrisService.BackgroundWorkers.IconUrl
{
    public class IconUrlWorker : BackgroundService
    {
        private readonly ILogger<IconUrlWorker> _logger;
        public IconUrlWorker(ILogger<IconUrlWorker> logger)
        {
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background worker is running.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await IconFetcher.RunIconUrlService();
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in worker loop");
                }
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
