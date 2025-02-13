using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public class SteamDiscoveryService
    {
        private string steamPath;
        private List<string> steamLibraries = new List<string>();

        public SteamDiscoveryService()
        {
            steamPath = GetSteamInstallPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                steamLibraries = GetSteamLibraryFolders();
            }
        }

        public List<Process> DiscoverSteamGames()
        {
            List<Process> processes = new List<Process>();

            foreach (var library in steamLibraries)
            {
                string steamAppsPath = Path.Combine(library, "steamapps");
                if (!Directory.Exists(steamAppsPath)) continue;

                foreach (var file in Directory.GetFiles(steamAppsPath, "appmanifest_*.acf"))
                {
                    Process steamProcess = ParseGameFromAcf(file, steamAppsPath);
                    if (steamProcess != null)
                    {
                        processes.Add(steamProcess);
                    }
                }
            }
            return processes;
        }

        private Process ParseGameFromAcf(string filePath, string steamAppsPath)
        {
            string gameName = null;
            string installDir = null;
            string appId = null;

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.Contains("\"appid\""))
                    appId = line.Split('"')[3];
                if (line.Contains("\"installdir\""))
                    installDir = line.Split('"')[3];
            }

            if (!string.IsNullOrEmpty(installDir))
            {
                string fullPath = Path.Combine(steamAppsPath, "common", installDir);
                if (Directory.Exists(fullPath))
                {
                    gameName = installDir;
                    var process = new Process(gameName, $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg", fullPath, FindExecutable(fullPath), false);
                    return process;
                }
            }
            return null;
        }

        private List<string> GetSteamLibraryFolders()
        {
            List<string> libraries = new List<string> { steamPath };

            string libraryFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFile)) return libraries;

            string[] lines = File.ReadAllLines(libraryFile);
            Regex regex = new Regex(@"^\s*""\d+""\s*""([^""]+)""");

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

        private string GetSteamInstallPath()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                return key?.GetValue("InstallPath") as string;
            }
        }

        private string FindExecutable(string gameFolder)
        {
            string[] exeFiles = Directory.GetFiles(gameFolder, "*.exe", SearchOption.AllDirectories);
            return exeFiles.Length > 0 ? Path.GetFileNameWithoutExtension(exeFiles[0]) : null;
        }
    }
}
