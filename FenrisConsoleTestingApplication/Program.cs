
using Fenris;
using Fenris.DiscoveryServices;
using System.Drawing;
using static Fenris.BlockSettings;



//var blockSettings = new BlockSettingsUrl();
//blockSettings.UrlBlock.Add("www.netflix.com", BlockType.Full);

////UserConfiguration.StoreBlockedWebsites(blockSettings);

//foreach (var block in blockSettings.UrlBlock)
//{
//    WebBlockerFirewall.AddFirewallBlock(block.Key, block.Value);
//}
var icon = await FavIconService.GetFaviconFromUrl("www.twitch.com");
Console.ReadKey();


public static class FavIconService
{
    private static readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5) // Prevent hanging on slow responses
    };

    public static async Task<Icon?> GetFaviconFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Normalize URL
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url; // Default to HTTPS first
        }

        try
        {
            // Try common favicon locations
            string[] faviconPaths =
            {
                    $"{url}/favicon.ico",
                    $"https://www.google.com/s2/favicons?domain={url}&sz=32", // Fallback to Google's service
                    $"{url}/apple-touch-icon.png" // Some sites use this instead
                };

            foreach (string faviconUrl in faviconPaths)
            {
                try
                {
                    using HttpResponseMessage response = await client.GetAsync(faviconUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] faviconBytes = await response.Content.ReadAsByteArrayAsync();
                        using var ms = new MemoryStream(faviconBytes);
                        return new Icon(ms); // Assuming Icon accepts a MemoryStream
                    }
                }
                catch (Exception)
                {
                    continue; // Try next option on failure
                }
            }
        }
        catch (Exception)
        {
            // Log exception if needed
        }

        // Return default icon or null if all attempts fail
        return null;
    }
}
