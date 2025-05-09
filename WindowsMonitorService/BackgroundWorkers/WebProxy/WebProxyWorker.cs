using Fenris.Models;
using Fenris.Storage;
using FenrisService.BackgroundWorkers.Process;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace WindowsMonitorService.BackgroundWorkers.WebProxy
{
    public class WebProxyWorker : BackgroundService
    {
        public bool UserConfigurationHasChanged = false;
        private readonly ILogger<WebProxyWorker> _logger;

        private Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>>? blockScheduleCache;
        private Dictionary<string, BlockData>? blockSettingUrlCache;

        private FileSystemWatcher blockSettingWatcher;
        private FileSystemWatcher blockSettingUrlWatcher;
        public WebProxyWorker(ILogger<WebProxyWorker> logger)
        {
            // Load the block settings from the configuration
            blockScheduleCache = UserConfiguration.LoadBlockSchedule().Result?.Block;
            blockSettingUrlCache = UserConfiguration.LoadBlockedWebsites().Result?.UrlBlock;
            _logger = logger;

            // Watch for changes in CommonApplicationData
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris");
            blockSettingWatcher = new FileSystemWatcher(settingsPath, "blockSchedule.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            blockSettingWatcher.Changed += OnSettingsFileChanged;
            blockSettingWatcher.EnableRaisingEvents = true;
            blockSettingUrlWatcher = new FileSystemWatcher(settingsPath, "blockedWebsites.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            blockSettingUrlWatcher.Changed += OnSettingsFileChanged;
            blockSettingUrlWatcher.EnableRaisingEvents = true;
        }

        private void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
        {
            UserConfigurationHasChanged = true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var proxy = CreateProxy();
            _logger.LogInformation("WebProxyWorker started.");
            try
            {
                // Wait until the service is requested to stop
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in WebProxyWorker.");
            }
            finally
            {
                _logger.LogInformation("Stopping proxy and restoring system settings...");
                proxy.Stop();
                proxy.DisableAllSystemProxies();
                _logger.LogInformation("WebProxyWorker stopped.");
            }
        }

        private ProxyServer CreateProxy()
        {
            var proxyServer = new ProxyServer();
            proxyServer.Start();
            proxyServer.BeforeRequest += ProxyServer_BeforeRequest;
            var explicitEndpoint = new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 8000, true);
            proxyServer.AddEndPoint(explicitEndpoint);
            proxyServer.SetAsSystemHttpProxy(explicitEndpoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndpoint);
            return proxyServer;
        }

        private async Task ProxyServer_BeforeRequest(object sender, Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            if (UserConfigurationHasChanged)
            {
                var schedule = await UserConfiguration.LoadBlockSchedule();
                blockScheduleCache = schedule?.Block;   
                var websites = await UserConfiguration.LoadBlockedWebsites();
                blockSettingUrlCache = websites?.UrlBlock;
                UserConfigurationHasChanged = false;
            }

            var url = e.HttpClient.Request.Url;

            if (blockSettingUrlCache == null)
                return;

            foreach (var kvp in blockSettingUrlCache)
            {
                var blockedKey = kvp.Key;
                var blockData = kvp.Value;

                if (!url.Contains(blockedKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (blockData.Type == BlockType.Full)
                {
                    var blockedHtml = string.Format(BlockedPageHtml.BlockedPage, "Blocked by FenrisBlock (Scheduled)");
                    e.Ok(blockedHtml);
                    return;
                }

                if (blockData.Type == BlockType.Schedule && blockScheduleCache != null)
                {
                    var now = DateTime.Now;
                    var day = now.DayOfWeek;
                    if (blockScheduleCache.TryGetValue(day, out var blockTimes))
                    {
                        foreach (var (start, end) in blockTimes)
                        {
                            if (now.TimeOfDay >= start && now.TimeOfDay <= end)
                            {
                                var blockedHtml = string.Format(BlockedPageHtml.BlockedPage, "Blocked by FenrisBlock (Scheduled)");
                                e.Ok(blockedHtml);
                                return;
                            }
                        }
                    }
                }
            }
            return;
        }
    }

    public static class BlockedPageHtml
    {
        public static string BlockedPage => @"
        <html>
            <head>
                <style>
                    body {
                        background-color: #333;
                        color: #ccc;
                        font-family: Arial, sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                    }
                    h1 {
                        font-size: 32px;
                        text-align: center;
                    }
                </style>
            </head>
            <body>
                <h1>{0}</h1>
            </body>
        </html>";
    }
}
