using Fenris.DiscoveryServices;
using Fenris.Models;
using FenrisService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FenrisService.BackgroundWorkers.Firewall
{
    internal static class FirewallRuleUpdater
    {
        public static Task RunUpdateFirewallRules(Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> block, Dictionary<string, BlockData> urlBlock)
        {
            DayOfWeek currentDay = DateTime.Now.DayOfWeek;
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            foreach (var item in urlBlock)
            {
                if (block.ContainsKey(currentDay))
                {
                    // Loop through the schedule for the current day and check if the current time is within any block time range
                    var blockTimes = block[currentDay];
                    bool shouldBlock = blockTimes.Any(block => currentTime >= block.BlockStart && currentTime <= block.BlockEnd);

                    if (shouldBlock)
                    {
                        // Enable firewall rule if the current time is within the block period
                        EnableFirewallRule(item.Key);
                    }
                    else
                    {
                        // Disable firewall rule if the current time is outside the block period
                        DisableFirewallRule(item.Key);
                    }
                }
                else
                {
                    // If there's no block schedule for the current day, disable the rule
                    DisableFirewallRule(item.Key);
                }
            }
            return Task.CompletedTask;
        }

        private static void EnableFirewallRule(string domain)
        {
            string ruleName = $"WebBlocker_{domain.Replace(".", "_")}";
            try
            {
                // Example PowerShell command to enable the rule
                string command = $"Set-NetFirewallRule -Name '{ruleName}' -Enabled True";
                ExecuteFirewallCommand(command);
                Console.WriteLine($"Enabled firewall rule for {domain}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to enable firewall rule for {domain}: {ex.Message}");
            }
        }

        private static void DisableFirewallRule(string domain)
        {
            string ruleName = $"WebBlocker_{domain.Replace(".", "_")}";
            try
            {
                // Example PowerShell command to disable the rule
                string command = $"Set-NetFirewallRule -Name '{ruleName}' -Enabled False";
                ExecuteFirewallCommand(command);
                Console.WriteLine($"Disabled firewall rule for {domain}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to disable firewall rule for {domain}: {ex.Message}");
            }
        }

        private static void ExecuteFirewallCommand(string firewallCommand)
        {
            try
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{firewallCommand}\"",
                    Verb = "runas",  // Run as administrator
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new System.Diagnostics.Process { StartInfo = processStartInfo })
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
                        Console.WriteLine($"Command failed with exit code: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing firewall command: {ex.Message}");
            }
        }
    }
}

