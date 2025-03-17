namespace Fenris.DiscoveryServices
{
    public interface IDiscoveryService
    {
        Task<List<Process>> DiscoverGames();
        Task<List<Process>> DiscoverSocials(); 
    }
}