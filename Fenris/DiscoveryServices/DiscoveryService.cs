using Fenris.Models;
using System.IO;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public class DiscoveryService
    {
        SteamDiscoveryService steamDiscoveryService = new();
        ApplicationDiscoveryService applicationDiscoveryService = new();
        public DiscoveryService()
        {

        }
        public async Task<List<Process>> DiscoverProcesses()
        {
            var processes = new List<Process>();

            var steamProcesses = await steamDiscoveryService.DiscoverSteamProcesses();
            if (steamProcesses != null)
            {
                processes.AddRange(steamProcesses);
            }
            var applicationProcesses = await applicationDiscoveryService.DiscoverProcesses();
            if (applicationProcesses != null)
            {
                processes.AddRange(applicationProcesses);
            }
            return processes;
        }
    }
}
