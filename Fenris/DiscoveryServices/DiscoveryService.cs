using System.IO;

namespace Fenris.DiscoveryServices
{
    public class DiscoveryService : IDiscoveryService
    {
        SteamDiscoveryService steamDiscoveryService;
        public DiscoveryService(SteamDiscoveryService steamDiscoveryService)
        {
            this.steamDiscoveryService = steamDiscoveryService;
        }
        public List<Process> DiscoverGames()
        {
            var processes = new List<Process>();

            var steamProcesses = steamDiscoveryService.DiscoverSteamGames();
            if (steamProcesses != null)
            {
                processes.AddRange(steamProcesses);
            }
            return processes;
        }

        public List<Process> DiscoverSocials()
        {
            throw new NotImplementedException();
        }
    }
}
