using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Fenris.Models;
using System.Management.Automation;

namespace Fenris.DiscoveryServices
{
    public class ApplicationDiscoveryService
    {
        public List<Process> DiscoverNonSteamGamesAndApps()
        {
            // Define directories to search (excluding system directories)
            var directoriesToSearch = new List<string>
            {
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            @"C:\Riot Games",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            };

            var processes = new List<Process>();

            foreach (var directory in directoriesToSearch)
            {
                processes.AddRange(DiscoverExecutables(directory));
            }

            return processes;
        }

        private List<Process> DiscoverExecutables(string directory)
        {
            var foundProcesses = new List<Process>();
            try
            {
                // Get all .exe files in the directory and subdirectories
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories)
                          .Where(file => IsValidExecutable(file)) // Apply filtering
                          .ToList();

                // Create Process objects for found executables and add to the list
                foreach (var exe in exeFiles)
                {
                    var processName = Path.GetFileNameWithoutExtension(exe);
                    foundProcesses.Add(new Process(processName, IconService.GetHighQualityIcon(exe), exe));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering executables in {directory}: {ex.Message}");
            }
            return foundProcesses;
        }

        private bool IsValidExecutable(string filePath)
        {
            string lowerPath = filePath.ToLowerInvariant();

            // Exclude executables if their path contains these keywords
            string[] ignoredKeywords = new string[]
            {
                "setup", "install", "redist", "prereq", "crashpad", "uninstall", "vc_redist",
                "update", "anticheat", "sentry", "pgomgr", "mspdbsrv", "link", "sguard", "cefsharp",
                "windows", "microsoft", "steam", "msbuild", "iis express", "internet explorer", "dotnet", 
                "realtek", "adobe", "oracle", "fenris", "overwolf", "nv", "error", "crash",
            };

            if (ignoredKeywords.Any(keyword => lowerPath.Contains(keyword)))
                return false;

            // Only allow executables in the main game folder or 1 subfolder deep
            //int depth = filePath.Split(Path.DirectorySeparatorChar).Length;
            //int rootDepth = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? "").Parent?.FullName.Split(Path.DirectorySeparatorChar).Length ?? 0;

            //return (depth - rootDepth) <= 2;
            return true;
        }
    }
}
