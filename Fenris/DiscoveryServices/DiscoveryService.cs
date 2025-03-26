using Fenris.Models;
using System.IO;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public class DiscoveryService : IDiscoveryService
    {
        SteamDiscoveryService steamDiscoveryService;
        ApplicationDiscoveryService applicationDiscoveryService;
        public DiscoveryService(SteamDiscoveryService steamDiscoveryService, ApplicationDiscoveryService applicationDiscoveryService)
        {
            this.steamDiscoveryService = steamDiscoveryService;
            this.applicationDiscoveryService = applicationDiscoveryService;
        }
        public async Task<List<Process>> DiscoverGames()
        {
            var processes = new List<Process>();

            var steamProcesses = steamDiscoveryService.DiscoverSteamGames();
            if (steamProcesses != null)
            {
                processes.AddRange(steamProcesses);
            }
            var applicationProcesses = applicationDiscoveryService.DiscoverNonSteamGamesAndApps();
            if (applicationProcesses != null)
            {
                processes.AddRange(applicationProcesses);
            }
            return processes;
        }
    }
}
