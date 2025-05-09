using Fenris.Models;
using Fenris.Storage;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

public class Program
{
    public static async Task Main(string[] args)
    {
        var proxyServer = new ProxyServer();
        proxyServer.BeforeRequest += ProxyServer_BeforeRequest;
        var explicitEndpoint = new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 8000, true);
        proxyServer.AddEndPoint(explicitEndpoint);
        proxyServer.Start();
        proxyServer.SetAsSystemHttpProxy(explicitEndpoint);
        proxyServer.SetAsSystemHttpsProxy(explicitEndpoint);

        Console.WriteLine("Proxy server running on port 8000. Press any key to stop...");
        Console.ReadKey();

        proxyServer.Stop();
    }

    private static Task ProxyServer_BeforeRequest(object sender, Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
    {
        var url = e.HttpClient.Request.Url;
        if (url.Contains("youtube"))
        {
            e.Ok("<html><body><h1>Blocked By FenrisBlock</h1></body></html>");
        }
        return Task.CompletedTask;
    }
}


