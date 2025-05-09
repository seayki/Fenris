using Fenris;
using Fenris.DiscoveryServices;
using FenrisService.BackgroundWorkers.IconUrl;
using FenrisService.BackgroundWorkers.Process;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using WindowsMonitorService.BackgroundWorkers.WebProxy;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(configure => configure.AddEventLog());
        services.AddHostedService<ProcessWorker>();
        services.AddHostedService<IconUrlWorker>();
        services.AddHostedService<WebProxyWorker>();
    })
    .UseWindowsService(options =>
    {
        options.ServiceName = "Fenris Proxy and Process Termination service";
    })
    .Build();
await host.RunAsync();
