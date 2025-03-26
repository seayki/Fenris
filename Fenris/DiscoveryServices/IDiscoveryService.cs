using Fenris.Models;

namespace Fenris.DiscoveryServices
{
    public interface IDiscoveryService
    {
        Task<List<Process>> DiscoverGames();
    }
}