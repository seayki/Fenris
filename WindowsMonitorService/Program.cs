using Fenris;
using Fenris.DiscoveryServices;
using FenrisService.BackgroundWorkers.Firewall;
using FenrisService.BackgroundWorkers.IconUrl;
using FenrisService.BackgroundWorkers.Process;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(configure => configure.AddEventLog());
        services.AddHostedService<ProcessWorker>();
        services.AddHostedService<FirewallWorker>();
        services.AddHostedService<IconUrlWorker>();
    })
    .UseWindowsService(options =>
    {
        options.ServiceName = "WindowsMonitorService";
    })
    .Build();
await host.RunAsync();
