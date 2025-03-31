using Fenris;
using Fenris.DiscoveryServices;
using FenrisService.BackgroundWorkers.Firewall;
using FenrisService.BackgroundWorkers.IconUrl;
using FenrisService.BackgroundWorkers.Process;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<ProcessWorker>();
        services.AddHostedService<FirewallWorker>();
        services.AddHostedService<IconUrlWorker>();
    })
    .Build();
await host.RunAsync();
