using Fenris.DiscoveryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris
{
    public static class WebBlocker
    {
        // Add a list of domains to the hosts file, redirecting to 127.0.0.1
        public static void AddWebBlocks(List<string> domains)
        {
            var hostPath = @"C:\Windows\System32\drivers\etc\hosts";
            var block = UserConfiguration.LoadBlockSettings().Result!.Block;
            if (domains == null || !domains.Any())
                throw new ArgumentException("Domains list cannot be null or empty.");

            try
            {
                string[] existingLines = File.Exists(hostPath) ? File.ReadAllLines(hostPath) : Array.Empty<string>();
                var newEntries = new List<string>();

                foreach (var domain in domains.Distinct()) // Remove duplicates
                {
                    if (string.IsNullOrWhiteSpace(domain))
                         
                        continue;

                    string entry = $"127.0.0.1 {domain}";
                    if (!existingLines.Any(line => line.Trim() == entry.Trim()))
                    {
                        newEntries.Add(entry);
                        Console.WriteLine($"Added hosts file entry: {entry}");
                    }
                    else
                    {
                        Console.WriteLine($"Entry already exists: {entry}");
                    }
                }

                if (newEntries.Any())
                {
                    File.AppendAllLines(hostPath, newEntries);
                    FlushDnsCache();
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: Run as administrator to modify hosts file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to hosts file: {ex.Message}");
            }
        }

        // Remove a specific domain from the hosts file based on the stored URL
        public static void RemoveBlock(string url)
        {
            var hostPath = @"C:\Windows\System32\drivers\etc\hosts";

            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("Error: Could not extract domain from URL.");
                    return;
                }

                string[] existingLines = File.Exists(hostPath) ? File.ReadAllLines(hostPath) : Array.Empty<string>();
                string entryToRemove = $"127.0.0.1 {url}";

                var updatedLines = existingLines.Where(line => line.Trim() != entryToRemove.Trim()).ToArray();

                if (updatedLines.Length < existingLines.Length)
                {
                    File.WriteAllLines(hostPath, updatedLines);
                    FlushDnsCache();
                    Console.WriteLine($"Removed hosts file entry: {entryToRemove}");
                }
                else
                {
                    Console.WriteLine($"No entry found to remove for: {url}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: Run as administrator to modify hosts file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from hosts file: {ex.Message}");
            }
        }


        // Helper method to flush DNS cache
        private static void FlushDnsCache()
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ipconfig /flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();
                Console.WriteLine("DNS cache flushed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing DNS cache: {ex.Message}");
            }
        }
    }
}
