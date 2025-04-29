using Fenris;
using Fenris.DiscoveryServices;
using FenrisService.BackgroundWorkers.Firewall;
using FenrisService.BackgroundWorkers.IconUrl;
using FenrisService.BackgroundWorkers.Process;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

var isWindowsService = !(Debugger.IsAttached || args.Contains("--console"));

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(services =>
    {
        services.AddHostedService<ProcessWorker>();
        services.AddHostedService<FirewallWorker>();
        services.AddHostedService<IconUrlWorker>();
    })
    .UseWindowsService(options =>
    {
        options.ServiceName = "FenrisService";
    })
    .Build();
await host.RunAsync();
