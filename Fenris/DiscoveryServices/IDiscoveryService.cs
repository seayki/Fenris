namespace Fenris.DiscoveryServices
{
    public interface IDiscoveryService
    {
        List<Process> DiscoverGames();
        List<Process> DiscoverSocials(); 
    }
}