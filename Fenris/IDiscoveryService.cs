namespace Fenris
{
    public interface IDiscoveryService
    {
        List<string> DiscoverGames();
        List<string> DiscoverSocials(); 
    }
}