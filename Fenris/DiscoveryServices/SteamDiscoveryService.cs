using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Process = Fenris.Models.Process;

namespace Fenris.DiscoveryServices
{
    public class SteamDiscoveryService
    {
        private readonly string? steamPath;
        private readonly List<string> steamLibraries = new();

        public SteamDiscoveryService()
        {
            steamPath = GetSteamInstallPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                try
                {
                    var libraries = GetSteamLibraryFolders(steamPath);
                    if (libraries != null)
                    {
                        steamLibraries = libraries;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to get Steam library folders: {ex.Message}");
                }
            }
        }

        public async Task<List<Process>> DiscoverSteamProcesses()
        {
            List<Process> steamGames = new();

            // Run file system operations on a background thread to avoid blocking the main thread
            await Task.Run(() =>
            {
                foreach (var library in steamLibraries)
                {
                    string steamAppsPath = Path.Combine(library, "steamapps");
                    if (!Directory.Exists(steamAppsPath)) continue;

                    foreach (var file in Directory.GetFiles(steamAppsPath, "appmanifest_*.acf"))
                    {
                        var gameInfo = ParseGameFromAcf(file, steamAppsPath);
                        if (gameInfo != null)
                        {
                            steamGames.Add(gameInfo);
                        }
                    }
                }
            });
            return steamGames;
        }

        private Process? ParseGameFromAcf(string filePath, string steamAppsPath)
        {
            var gameName = "";
            var installDir = "";
            var appId = "";
            var executableName = "";

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.Contains("\"appid\""))
                    appId = line.Split('"')[3];
                if (line.Contains("\"installdir\""))
                    installDir = line.Split('"')[3];
            }

            if (!string.IsNullOrEmpty(installDir) && !string.IsNullOrEmpty(appId))
            {
                string fullPath = Path.Combine(steamAppsPath, "common", installDir);
                if (Directory.Exists(fullPath))
                {
                    gameName = installDir;
                    executableName = FindExecutable(fullPath); // Get the actual executable name
                    if (executableName != null)
                        return new Process(gameName, $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg", executableName);
                    else
                        return null;
                }
            }
            return null;
        }

        private List<string>? GetSteamLibraryFolders(string steamPath)
        {
            List<string> libraries = new() { steamPath };
            string libraryFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(libraryFile)) return libraries;

            string[] lines = File.ReadAllLines(libraryFile);
            Regex regex = new(@"^\s*""\d+""\s*""([^""]+)""");

            foreach (var line in lines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    libraries.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
                }
            }
            return libraries;
        }
        private string? GetSteamInstallPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                return key?.GetValue("InstallPath") as string;
            }
            return null;
        }

        private string? FindExecutable(string gameFolder)
        {
            string[] exeFiles = Directory.GetFiles(gameFolder, "*.exe", SearchOption.AllDirectories);
            return exeFiles.Length > 0 ? Path.GetFileNameWithoutExtension(exeFiles[0]) : null;
        }
    }
}
