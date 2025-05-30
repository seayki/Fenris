using Fenris.Models;
using Fenris.Storage;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using System.Net;

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
        private ProxyServer? proxyServer;

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
            _logger = logger;

            try
            {
                string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris");
                Directory.CreateDirectory(settingsPath);
                _logger.LogInformation("Ensured Fenris directory exists: {Path}", settingsPath);

                blockScheduleCache = UserConfiguration.LoadBlockSchedule().Result?.Block;
                blockSettingUrlCache = UserConfiguration.LoadBlockedWebsites().Result?.UrlBlock;

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

                _logger.LogInformation("WebProxyWorker initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize WebProxyWorker");
                throw;
            }
        }

        private void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Configuration file changed: {File}", e.FullPath);
            UserConfigurationHasChanged = true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WebProxyWorker starting...");
            try
            {
                proxyServer = CreateProxy();
                _logger.LogInformation("WebProxyWorker started successfully");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("WebProxyWorker cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in WebProxyWorker");
                throw;
            }
            finally
            {
                _logger.LogInformation("Stopping proxy and restoring system settings...");
                if (proxyServer != null)
                {
                    proxyServer.Stop();
                    proxyServer.DisableAllSystemProxies();
                    proxyServer.Dispose();
                    _logger.LogInformation("Proxy stopped and system settings restored");
                }
                _logger.LogInformation("WebProxyWorker stopped");
            }
        }

        private ProxyServer CreateProxy()
        {
            try
            {
                var proxyServer = new ProxyServer();
                _logger.LogInformation("Starting proxy server...");
                proxyServer.Start();

                // Add event handlers
                proxyServer.BeforeRequest += ProxyServer_BeforeRequest;
                var explicitEndpoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, false);
                _logger.LogInformation("Adding explicit endpoint: {IpAddress}:{Port}, DecryptSsl: {DecryptSsl}",
                    explicitEndpoint.IpAddress, explicitEndpoint.Port, explicitEndpoint.DecryptSsl);
                proxyServer.AddEndPoint(explicitEndpoint);
                explicitEndpoint.BeforeTunnelConnectRequest += ProxyServer_BeforeTunnelConnect;

                _logger.LogInformation("Setting system HTTP/HTTPS proxy...");
                proxyServer.SetAsSystemHttpProxy(explicitEndpoint);
                proxyServer.SetAsSystemHttpsProxy(explicitEndpoint);

                // Verify system proxy settings
                _logger.LogInformation("System proxy settings applied. Checking current proxy configuration...");
                var currentProxy = WebRequest.GetSystemWebProxy();
                var testUri = new Uri("http://example.com");
                var proxyUri = currentProxy.GetProxy(testUri);
                _logger.LogInformation("Current system proxy for {TestUri}: {ProxyUri}", testUri, proxyUri);

                _logger.LogInformation("Proxy server created and configured");
                return proxyServer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create proxy server");
                throw;
            }
        }

        private async Task ProxyServer_BeforeRequest(object sender, SessionEventArgs e)
        {
            try
            {
                // Log every incoming request
                _logger.LogInformation("Intercepted request: URL={Url}, Method={Method}, IsHttps={IsHttps}",
                    e.HttpClient.Request.Url, e.HttpClient.Request.Method, e.HttpClient.Request.IsHttps);

                if (UserConfigurationHasChanged)
                {
                    _logger.LogInformation("Reloading configuration due to change...");
                    var schedule = await UserConfiguration.LoadBlockSchedule();
                    blockScheduleCache = schedule?.Block;
                    var websites = await UserConfiguration.LoadBlockedWebsites();
                    blockSettingUrlCache = websites?.UrlBlock;
                    UserConfigurationHasChanged = false;
                    _logger.LogInformation("Configuration reloaded");
                }

                if (blockSettingUrlCache == null)
                {
                    _logger.LogWarning("blockSettingUrlCache is null, skipping request processing");
                    return;
                }

                var url = e.HttpClient.Request.Url;
                foreach (var kvp in blockSettingUrlCache)
                {
                    var blockedKey = kvp.Key;
                    var blockData = kvp.Value;

                    if (!url.Contains(blockedKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (blockData.Type == BlockType.Full)
                    {
                        _logger.LogInformation("Blocking URL (Full): {Url}", url);
                        e.Ok(htmlContent);
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
                                    _logger.LogInformation("Blocking URL (Schedule): {Url}", url);
                                    e.Ok(htmlContent);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing proxy request for URL: {Url}", e.HttpClient.Request.Url);
            }
        }

        private async Task ProxyServer_BeforeTunnelConnect(object sender, TunnelConnectSessionEventArgs e)
        {
            try
            {
                // Log CONNECT tunnel requests for HTTPS
                _logger.LogInformation("Intercepted CONNECT request: Host={Host}, IsHttps={IsHttps}",
                    e.HttpClient.Request.RequestUri.Host, e.HttpClient.Request.IsHttps);

                if (UserConfigurationHasChanged)
                {
                    _logger.LogInformation("Reloading configuration due to change...");
                    var schedule = await UserConfiguration.LoadBlockSchedule();
                    blockScheduleCache = schedule?.Block;
                    var websites = await UserConfiguration.LoadBlockedWebsites();
                    blockSettingUrlCache = websites?.UrlBlock;
                    UserConfigurationHasChanged = false;
                    _logger.LogInformation("Configuration reloaded");
                }

                if (blockSettingUrlCache == null)
                {
                    _logger.LogWarning("blockSettingUrlCache is null, skipping CONNECT processing");
                    return;
                }

                var host = e.HttpClient.Request.RequestUri.Host;
                foreach (var kvp in blockSettingUrlCache)
                {
                    var blockedKey = kvp.Key;
                    var blockData = kvp.Value;

                    if (!host.Contains(blockedKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (blockData.Type == BlockType.Full)
                    {
                        _logger.LogInformation("Blocking CONNECT (Full): {Host}", host);
                        e.DenyConnect = true;
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
                                    _logger.LogInformation("Blocking CONNECT (Schedule): {Host}", host);
                                    e.DenyConnect = true;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CONNECT request for Host: {Host}", e.HttpClient.Request.RequestUri.Host);
            }
        }
    }
}