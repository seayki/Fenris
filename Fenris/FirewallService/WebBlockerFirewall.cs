using Fenris.Models;
using Fenris.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Fenris.FirewallService
{
    public static class WebBlockerFirewall
    {
        public static async Task AddFirewallBlock(string domain, BlockType type)
        {
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain cannot be empty or null.");
            // Resolve domain to all associated IP addresses dynamically
            var countryCode = await GetCountryCodeFromIP();
            var ipAddresses = await GetIpAddresses(domain, countryCode);
            if (ipAddresses == null || !ipAddresses.Any())
            {
                Console.WriteLine($"Failed to resolve any IPs for domain: {domain}");
                return;
            }

            string ruleName = $"WebBlocker_{domain.Replace(".", "_")}";

            // Step 1: Create the rule with the first IP
            string firstIp = ipAddresses[0];
            string createCommand = $"New-NetFirewallRule -DisplayName 'FenrisBlock_{ruleName}' -Name '{ruleName}' -Direction Outbound -Action Block -RemoteAddress '{firstIp}' -Enabled True";

            // Step 2: If there are more IPs, update the rule with the full list
            string ipList = string.Join("','", ipAddresses); // Format as 'ip1','ip2','ip3' for PowerShell array
            var updateCommand = ipAddresses.Count > 1
                ? $"Set-NetFirewallRule -Name '{ruleName}' -RemoteAddress @('{ipList}')"
                : null;

            await Task.Run(() =>
            {
                ExecuteFirewallCommand(createCommand, domain);
                if (updateCommand != null)
                {
                    ExecuteFirewallCommand(updateCommand, domain);
                }
            });
        }

        static async Task<string> GetCountryCodeFromIP()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string response = await client.GetStringAsync("https://ipinfo.io/json");
                    using JsonDocument doc = JsonDocument.Parse(response);
                    return doc.RootElement.GetProperty("country").GetString() ?? "DK";
                }
                catch
                {
                    return "DK";
                }
            }
        }

        // Helper method to execute PowerShell command
        private static void ExecuteFirewallCommand(string firewallCommand, string domain)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{firewallCommand}\"",
                Verb = "runas",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = psi })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine($"PowerShell Output: {output}");
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"PowerShell Error: {error}");
                if (process.ExitCode != 0)
                    Console.WriteLine($"PowerShell command failed with exit code: {process.ExitCode}");
                else
                    Console.WriteLine($"Firewall rule successfully added or removed for {domain}");
            }
        }

        // Helper method to resolve all IPs for a url and different domain endings
        private static async Task<List<string>> GetIpAddresses(string url, string countryCode)
        {
            try
            {
                countryCode = ReturnCountryCode(countryCode);
                List<string> combined = new()
                {
                url
                };

                if (url.EndsWith(".com"))
                {
                    combined.Add(url.Replace(".com", countryCode));
                }

                var ipTasks = combined.Select(async domain =>
                {
                    try
                    {
                        var ipList = await Dns.GetHostAddressesAsync(domain);
                        return ipList
                            .Where(a => a.AddressFamily == AddressFamily.InterNetwork ||
                                        a.AddressFamily == AddressFamily.InterNetworkV6)
                            .Select(a => a.ToString())
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error resolving IPs for {domain}: {ex.Message}");
                        return new List<string>();
                    }
                });

                var results = await Task.WhenAll(ipTasks);
                var addresses = results.SelectMany(list => list).ToList();

                Console.WriteLine($"Resolved IPs for {url}: {string.Join(", ", addresses)}");
                return addresses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving IPs for {url}: {ex.Message}");
                return new List<string>();
            }
        }

        static string ReturnCountryCode(string countryCode)
        {
            return countryCode switch
            {
                "US" => ".us",
                "UK" => ".uk",
                "DE" => ".de",
                "CA" => ".ca",
                "AU" => ".au",
                "FR" => ".fr",
                "IN" => ".in",
                "DK" => ".dk",
                _ => ".dk" // Default DK
            };
        }

        public static async Task RemoveFirewallBlock(string domain)
        {
            string firewallRuleName = $"FenrisBlock_WebBlocker_{domain.Replace(".", "_")}";
            string firewallCommand = $"Remove-NetFirewallRule -DisplayName '{firewallRuleName}'";
            await Task.Run(() => ExecuteFirewallCommand(firewallCommand, domain));
        }
    }
}
