using Fenris.DiscoveryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fenris
{
    public class WebBlocker
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly Dictionary<string, List<string>> blockedUrls = new();
        private readonly UserConfiguration userConfig = new();
        public WebBlocker()
        {
            
        }
        public void AddBlock(List<string> urls)
        {
            foreach (var url in urls)
            {
                var ipAddresses = RetrieveIPRange(url);
                if (!ipAddresses.Any()) continue;

                blockedUrls[url] = ipAddresses; // Track URL and associated IPs

                foreach (var address in ipAddresses)
                {
                    string ruleName = $"Block_{address}";
                    string command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block remoteip={address}";
                    ExecuteCommand(command);
                }
            }
            userConfig.StoreBlockedWebsites(new BlockSettingUrl(blockedUrls));
        }

        public void RemoveFirewallBlock(string url)
        {
            var urlIPs = userConfig.ReceiveUrlIps(url);
            foreach (var ip in urlIPs)
            {
                RemoveFirewallRuleByIp(ip);
            }
        }
        private void RemoveFirewallRuleByIp(string ip)
        {
            string ruleName = $"Block_{ip}";
            string command = $"netsh advfirewall firewall delete rule name=\"{ruleName}\"";
            ExecuteCommand(command);
        }
        private async Task<Icon?> GetFaviconFromUrl(string url)
        {
            try
            {
                string faviconUrl = $"http://{url}/favicon.ico";
                using HttpResponseMessage response = await client.GetAsync(faviconUrl);
                if (response.IsSuccessStatusCode)
                {
                    byte[] faviconBytes = await response.Content.ReadAsByteArrayAsync();
                    using var ms = new MemoryStream(faviconBytes);
                    return new Icon(ms);
                }
            }
            catch
            {
                // Ignore favicon retrieval failures
            }
            return null;
        }

        private void ExecuteCommand(string command)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(processInfo)!)
            {
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Command failed: {output}");
                }
            }
        }

        public List<string> RetrieveIPRange(string url)
        {
            try
            {
                Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                string host = uri.Host;
                var addresses = Dns.GetHostAddresses(host);
                return addresses.Select(address => address.ToString()).ToList();
            }
            catch
            {
                return new List<string>(); // Return empty list if DNS lookup fails
            }
        }
    }
}
