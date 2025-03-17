using Fenris;
using Fenris.DiscoveryServices;
using FenrisService.BackgroundWorkers.Process;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenrisService.BackgroundWorkers.Firewall
{
    internal class FirewallWorker : BackgroundService
    {
        private readonly ILogger<ProcessWorker> _logger;
        private Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> blockSettingCache;
        private Dictionary<string, BlockType> blockSettingUrlCache;
        private CancellationTokenSource _cts;
        private bool firstTime = true;
        private FileSystemWatcher blockSettingWatcher;
        private FileSystemWatcher blockSettingUrlWatcher;
        public FirewallWorker(ILogger<ProcessWorker> logger)
        {
            _logger = logger;
            _cts = new CancellationTokenSource();

            // Load the block settings from the configuration
            blockSettingCache = UserConfiguration.LoadBlockSettings().Result.Block;
            blockSettingUrlCache = UserConfiguration.LoadBlockedWebsites().Result.UrlBlock;
            

            // Watch for changes in LocalApplicationData
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris");
            blockSettingWatcher = new FileSystemWatcher(settingsPath, "blockSettings.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            blockSettingWatcher.Changed += OnSettingsFileChanged<BlockSettings>;
            blockSettingWatcher.EnableRaisingEvents = true;
            blockSettingUrlWatcher = new FileSystemWatcher(settingsPath, "blockedWebsites.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            blockSettingUrlWatcher.Changed += OnSettingsFileChanged<BlockSettingsUrl>;
            blockSettingUrlWatcher.EnableRaisingEvents = true;

        }

        private void OnSettingsFileChanged<T>(object sender, FileSystemEventArgs e)
        {
            try
            {
                _logger.LogInformation("Detected changes in {file}, reloading...", e.Name);

                // Check for the type and load appropriate settings
                if (typeof(T) == typeof(BlockSettings))
                {
                    blockSettingCache = UserConfiguration.LoadBlockSettings().Result?.Block ?? new Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>>();
                }
                else
                {
                    blockSettingUrlCache = UserConfiguration.LoadBlockedWebsites().Result?.UrlBlock ?? new Dictionary<string, BlockType>();
                }

                // Restart the process of checking firewall rules
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading block settings from {file}", e.Name);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nextEventTime = GetNextEventTime(blockSettingCache);
                    var now = DateTimeOffset.Now;
                    var delay = nextEventTime - now;

                    // Log the planned action
                    _logger.LogInformation("Next firewall update at {time}", nextEventTime);

                    // Trim web urls to only apply for scheduled blocks
                    foreach (var item in blockSettingUrlCache)
                    {
                        if (item.Value != BlockType.Schedule)
                        {
                            blockSettingUrlCache.Remove(item.Key);
                        }
                    }

                    if (firstTime || delay.TotalMilliseconds <= 10000)
                    {
                        await FirewallRuleUpdater.RunUpdateFirewallRules(blockSettingCache, blockSettingUrlCache);
                        _logger.LogInformation("Firewall rules updated at: {time}", DateTimeOffset.Now);
                        firstTime = false;
                    }
                    else
                    {
                        await Task.Delay(delay, CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cts.Token).Token);
                        await FirewallRuleUpdater.RunUpdateFirewallRules(blockSettingCache, blockSettingUrlCache);
                        _logger.LogInformation("Firewall rules updated at: {time}", DateTimeOffset.Now);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancellation triggered by settings change, loop continues
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in worker loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private DateTimeOffset GetNextEventTime(Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> blockSettings)
        {
            var now = DateTimeOffset.Now;
            var currentDay = now.DayOfWeek;

            if (!blockSettings.TryGetValue(currentDay, out var todaySchedule))
            {
                todaySchedule = new List<(TimeSpan, TimeSpan)>();
            }

            DateTimeOffset? nextEventTime = null;

            foreach (var (start, end) in todaySchedule)
            {
                var startTimeToday = now.Date + start;
                var endTimeToday = now.Date + end;

                if (now < startTimeToday && (nextEventTime == null || startTimeToday < nextEventTime))
                {
                    nextEventTime = startTimeToday;
                }
                else if (now < endTimeToday && (nextEventTime == null || endTimeToday < nextEventTime))
                {
                    nextEventTime = endTimeToday;
                }
            }

            if (nextEventTime == null)
            {
                for (int i = 1; i <= 7; i++)
                {
                    var nextDay = now.AddDays(i).DayOfWeek;
                    if (blockSettings.TryGetValue(nextDay, out var nextDaySchedule) && nextDaySchedule.Any())
                    {
                        nextEventTime = now.Date.AddDays(i) + nextDaySchedule[0].BlockStart;
                        break;
                    }
                }
            }

            return nextEventTime ?? now.AddDays(1);
        }
    }
}

