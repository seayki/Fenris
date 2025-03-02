using System.IO;

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
        public List<Process> DiscoverGames()
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

        public List<Process> DiscoverSocials()
        {
            throw new NotImplementedException();
        }
    }
}
