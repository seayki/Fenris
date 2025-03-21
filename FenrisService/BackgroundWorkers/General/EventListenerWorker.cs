using Fenris;
using FenrisService.BackgroundWorkers.Process;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenrisService.BackgroundWorkers.General
{
    public class EventListenerWorker : BackgroundService
    {
        private readonly ILogger<EventListenerWorker> _logger;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningTasks;
        public EventListenerWorker(ILogger<EventListenerWorker> logger)
        {
            _logger = logger;
            _runningTasks = new ConcurrentDictionary<string, CancellationTokenSource>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background worker is running.");
            return Task.CompletedTask; // No main loop required, tasks are managed separately
        }
    }
}
