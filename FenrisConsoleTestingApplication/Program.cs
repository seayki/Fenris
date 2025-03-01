
using static Fenris.BlockSettings;
using Fenris;
using Fenris.DiscoveryServices;

var steamDiscoveryService = new SteamDiscoveryService();
var applicationDiscoveryService = new ApplicationDiscoveryService();
var discoveryService = new DiscoveryService(steamDiscoveryService, applicationDiscoveryService);
List<Process> processes = discoveryService.DiscoverGames();

Console.WriteLine("Discovered Processes:");
foreach (var process in processes)
{
    Console.WriteLine($"- {process.Name}");
    Console.WriteLine($"  ID: {process.Executable}");
    Console.WriteLine($"  Icon: {process.IconUrl}");
}