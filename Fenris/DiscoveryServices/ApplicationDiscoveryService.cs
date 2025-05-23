using Fenris.IconServices;
using Fenris.Models;
using Fenris.Storage;

namespace Fenris.DiscoveryServices
{
    public class ApplicationDiscoveryService
    {
        // Make this method asynchronous to support async operations
        public async Task<List<Process>> DiscoverProcesses()
        {
            var directoriesToSearch = new List<string>
            {
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                @"C:\Riot Games",
                @"C:\Games\World_of_Tanks",
                @"C:\Games\World_of_Warships",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            };

            var processes = new List<Process>();

            // Use async operations for directories discovery
            var tasks = directoriesToSearch.Select(directory => DiscoverExecutables(directory));
            var results = await Task.WhenAll(tasks);

            // Flatten the results and add them to the processes list
            foreach (var result in results)
            {
                processes.AddRange(result);
            }
            var uniqueProcesses = processes
                .GroupBy(p => p.Name)
                .Select(g => g.First())
                .OrderBy(p => p.Name)
                .ToList();
            return uniqueProcesses;
        }

        // Convert to async method
        private async Task<List<Process>> DiscoverExecutables(string directory)
        {
            var foundProcesses = new List<Process>();
            try
            {
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories)
                                        .Where(file => IsValidExecutable(file))
                                        .ToList();

                foreach (var exe in exeFiles)
                {
                    var processName = Path.GetFileNameWithoutExtension(exe);
                    string? iconPath = await GetIconPath(exe);
                    foundProcesses.Add(new Process(processName, iconPath, processName));
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

            string[] ignoredKeywords = new[]
            {
                "setup", "install", "redist", "prereq", "crashpad", "uninstall", "vc_redist",
                "update", "anticheat", "sentry", "pgomgr", "mspdbsrv", "link", "sguard", "cefsharp",
                "windows", "microsoft", "steam", "msbuild", "iis express", "internet explorer", "dotnet",
                "realtek", "adobe", "oracle", "fenris", "overwolf", "nv", "error", "crash", "patcher", "repair", "helper", "launcher"
            };
            return !ignoredKeywords.Any(keyword => lowerPath.Contains(keyword));
        }

        private async Task<string?> GetIconPath(string exePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(exePath);
            string iconFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "Icons");
            string iconPath = Path.Combine(iconFolder, $"{fileName}.png");

            // If icon already exists in the cache, return its path
            if (File.Exists(iconPath))
            {
                return iconPath;
            }
            try
            {
                var iconBitmap = HighResolutionIconExtractor.GetHighResIcon(exePath, 256);
                if (iconBitmap != null)
                {
                    return await UserConfiguration.SaveIconToLocalFolder(iconBitmap, exePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting high-quality icon for {exePath}: {ex.Message}");
            }

            return null;
        }
    }
}
