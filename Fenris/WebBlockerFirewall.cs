using Fenris.DiscoveryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;

namespace Fenris
{
    public static class WebBlockerFirewall
    {
        public static void AddFirewallBlock(string domain, BlockType type)
        {
            var block = UserConfiguration.LoadBlockSettings().Result!.Block;
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain cannot be empty or null.");

            // Resolve domain to all associated IP addresses dynamically
            var ipAddresses = GetIpAddresses(domain);
            if (ipAddresses == null || !ipAddresses.Any())
            {
                Console.WriteLine($"Failed to resolve any IPs for domain: {domain}");
                return;
            }

            string ruleName = $"WebBlocker_{domain.Replace(".", "_")}";
            bool isScheduled = (type == BlockType.Schedule);
            if (isScheduled && (block == null || block.Count == 0))
                throw new ArgumentException("Block schedule must be defined.");

            // Step 1: Create the rule with the first IP
            string firstIp = ipAddresses[0];
            string createCommand = $"New-NetFirewallRule -DisplayName 'FenrisBlock_{ruleName}' -Name '{ruleName}' -Direction Outbound -Action Block -RemoteAddress '{firstIp}' -Enabled {(isScheduled ? "False" : "True")}";

            // Step 2: If there are more IPs, update the rule with the full list
            string ipList = string.Join("','", ipAddresses); // Format as 'ip1','ip2','ip3' for PowerShell array
            string updateCommand = ipAddresses.Count > 1
                ? $"Set-NetFirewallRule -Name '{ruleName}' -RemoteAddress @('{ipList}')"
                : null;

            // Execute the create command
            ExecuteFirewallCommand(createCommand, domain);

            // If there are more IPs, execute the update command
            if (updateCommand != null)
            {
                ExecuteFirewallCommand(updateCommand, domain);
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

        // Helper method to dynamically resolve all IPs for a domain
        private static List<string> GetIpAddresses(string domain)
        {
            try
            {
                var addresses = System.Net.Dns.GetHostAddresses(domain)
                    .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || // IPv4
                                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) // IPv6
                    .Select(a => a.ToString())
                    .ToList();
                Console.WriteLine($"Resolved IPs for {domain}: {string.Join(", ", addresses)}");
                return addresses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving IPs for {domain}: {ex.Message}");
                return new List<string>();
            }
        }

        public static void RemoveFirewallBlock(string domain)
        {
            string firewallRuleName = $"FenrisBlock_WebBlocker_{domain.Replace(".", "_")}";
            string firewallCommand = $"Remove-NetFirewallRule -DisplayName '{firewallRuleName}'";
            ExecuteFirewallCommand(firewallCommand, domain);
        }
    }
}
