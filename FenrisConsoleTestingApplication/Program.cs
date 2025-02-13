

using Fenris;
using Fenris.DiscoveryServices;
using static Fenris.BlockSettings;

var steamDiscoveryService = new SteamDiscoveryService();
var discoveryService = new DiscoveryService(steamDiscoveryService);
List<Process> processes = discoveryService.DiscoverGames();
processes.ForEach(p => p.AddBlock(BlockDefaults.Default));

var processTerminator = new ProcessTerminator(processes);

Console.WriteLine("Discovered Processes:");
foreach (var process in processes)
{
    Console.WriteLine($"- {process.Name}");
    Console.WriteLine($"  Path: {process.InstallPath}");
    Console.WriteLine($"  Executable: {process.Executable}");
    Console.WriteLine($"  Icon: {process.IconUrl}");
}
processTerminator.RunTerminateProcessBackgroundService();