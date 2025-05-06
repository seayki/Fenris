using Fenris.Models;
using Fenris.Storage;

public class Program
{
    public static async Task Main(string[] args)
    {
        var url = new BlockSettingsUrl("netflix.com", BlockType.Full);
        try
        {
            await UserConfiguration.StoreBlockedWebsites(url);
            Console.WriteLine("Blocked websites loaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}


