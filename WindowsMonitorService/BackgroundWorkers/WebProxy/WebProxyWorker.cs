using Fenris.Models;
using Fenris.Storage;
using System.Security.Cryptography.X509Certificates;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;

namespace FenrisService.BackgroundWorkers.WebProxy
{
    public class WebProxyWorker : BackgroundService
    {
        public bool UserConfigurationHasChanged = false;
        private readonly ILogger<WebProxyWorker> _logger;

        private Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>>? blockScheduleCache;
        private Dictionary<string, BlockData>? blockSettingUrlCache;

        private FileSystemWatcher blockSettingWatcher;
        private FileSystemWatcher blockSettingUrlWatcher;

        string htmlContent = @"
               <html>
               <head>
                   <style>
                       html, body {
                           height: 100%;
                           margin: 0;
                       }
                       body {
                           display: flex;
                           align-items: center;
                           justify-content: center;
                           font-family: sans-serif;
                           background-color: #f9f9f9;
                       }
                       h1 {
                           text-align: center;
                           color: #444;
                           margin-bottom: 100px;
                       }
                   </style>
               </head>
               <body>
                   <h1>Blocked by FenrisBlock.</h1>
               </body>
               </html>";

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

        private bool IsBlockedDomain(string host) =>
            blockSettingUrlCache.Keys.Any(k => host.Contains(k, StringComparison.OrdinalIgnoreCase));

        private bool IsBlockedUrl(string url) =>
            blockSettingUrlCache!.Keys.Any(k => url.Contains(k, StringComparison.OrdinalIgnoreCase));

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
            var proxyServer = new ProxyServer
            {
                EnableHttp2 = true,
                ForwardToUpstreamGateway = true
            };

            proxyServer.BeforeRequest += ProxyServer_BeforeRequest;

            var explicitEndPoint = new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 8000, decryptSsl: false); // Don't intercept HTTPS
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

            return proxyServer;
        }

        private async Task ProxyServer_BeforeRequest(object sender, SessionEventArgs e)
        {
            if (UserConfigurationHasChanged)
            {
                var schedule = await UserConfiguration.LoadBlockSchedule();
                blockScheduleCache = schedule?.Block;
                var websites = await UserConfiguration.LoadBlockedWebsites();
                blockSettingUrlCache = websites?.UrlBlock;
                UserConfigurationHasChanged = false;
            }

            if (blockSettingUrlCache == null)
                return;

            string host = e.HttpClient.Request.RequestUri?.Host ?? "";
            string fullUrl = e.HttpClient.Request.Url;

            foreach (var kvp in blockSettingUrlCache)
            {
                string blockedKey = kvp.Key;
                var blockData = kvp.Value;

                bool isBlockedHost = host.Contains(blockedKey, StringComparison.OrdinalIgnoreCase);
                bool isBlockedUrl = fullUrl.Contains(blockedKey, StringComparison.OrdinalIgnoreCase);

                if (e.HttpClient.Request.Method == "CONNECT" && isBlockedHost)
                {
                    if (ShouldBlockNow(blockData))
                    {
                        e.Ok(htmlContent);
                        return;
                    }
                }

                else
                {
                    string url = e.HttpClient.Request.Url;
                    if (IsBlockedUrl(url) && ShouldBlockNow(blockData))
                    {
                        e.Ok(htmlContent);
                        return;
                    }
                }
            }
        }

        private bool ShouldBlockNow(BlockData blockData)
        {
            if (blockData.Type == BlockType.Full)
                return true;

            if (blockData.Type == BlockType.Schedule && blockScheduleCache != null)
            {
                var now = DateTime.Now;
                var day = now.DayOfWeek;
                if (blockScheduleCache.TryGetValue(day, out var blockTimes))
                {
                    foreach (var (start, end) in blockTimes)
                    {
                        if (now.TimeOfDay >= start && now.TimeOfDay <= end)
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
