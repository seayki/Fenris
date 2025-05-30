using FenrisService.BackgroundWorkers.IconUrl;
using FenrisService.BackgroundWorkers.Process;
using FenrisService.BackgroundWorkers.WebProxy;


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
        options.ServiceName = "Windows Monitor Service";
    })
    .Build();
await host.RunAsync();
