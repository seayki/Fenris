using Fenris.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public class ApplicationDiscoveryService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        public List<Process> DiscoverNonSteamGamesAndApps()
        {
            // Define directories to search (excluding system directories)
            var directoriesToSearch = new List<string>
            {
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            @"C:\Riot Games",
<<<<<<< Updated upstream
=======
            @"C:\Games\World_of_Tanks",
            @"C:\Games\World_of_Warships",
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
                    foundProcesses.Add(new Process(processName, IconService.GetHighQualityIcon(exe), exe));
=======
                    foundProcesses.Add(new Process(processName, GetHighQualityIcon(exe), exe));
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
                "update", "anticheat", "sentry", "pgomgr", "mspdbsrv", "link", "sguard", "cefsharp",
                "windows", "microsoft", "steam", "msbuild", "iis express", "internet explorer", "dotnet", 
                "realtek", "adobe", "oracle", "fenris", "overwolf", "nv", "error", "crash",
=======
                "update", "anticheat", "sentry", "pgomgr", "mspdbsrv", "sguard", "cefsharp",
                "windows", "microsoft", "steam", "msbuild", "iis express", "internet explorer", 
                "dotnet", "realtek", "adobe", "oracle", "fenris", "overwolf", "blizzard", 
                "crash", "nv", "helper", "version", "launcher", "updater", "patch", "patcher", "repair"
>>>>>>> Stashed changes
            };

            if (ignoredKeywords.Any(keyword => lowerPath.Contains(keyword)))
                return false;

            // Only allow executables in the main game folder or 1 subfolder deep
            //int depth = filePath.Split(Path.DirectorySeparatorChar).Length;
            //int rootDepth = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? "").Parent?.FullName.Split(Path.DirectorySeparatorChar).Length ?? 0;

            //return (depth - rootDepth) <= 2;
            return true;
        }
<<<<<<< Updated upstream
=======

        public static string? GetHighQualityIcon(string exePath)
        {
            if (!File.Exists(exePath))
            {
                return null;
            }

            string cachePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(exePath)}.png");

            try
            {
                var icon = GetLargeIcon(exePath);
                if (icon == null)
                {
                    return null;
                }

                using (var bitmap = icon.ToBitmap())
                {
                    bitmap.Save(cachePath, System.Drawing.Imaging.ImageFormat.Png); // PNG for better quality
                }

                return cachePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting high-quality icon: {ex.Message}");
                return null;
            }
        }

        private static Icon GetLargeIcon(string filePath)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImgLarge = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
            return hImgLarge != IntPtr.Zero ? Icon.FromHandle(shinfo.hIcon) : null;
        }
>>>>>>> Stashed changes
    }
}
