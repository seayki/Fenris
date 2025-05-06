using Fenris.Models;
using Fenris.Storage;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace FenrisService.BackgroundWorkers.Process
{

    public static class ProcessTerminator
    {
        public static async Task RunTerminateProcess()
        {
            var blockedApps = await UserConfiguration.LoadBlockedApps();
            var blockSchedule = await UserConfiguration.LoadBlockSchedule();
            if (blockedApps == null || blockSchedule == null) return;

            var allProcesses = System.Diagnostics.Process.GetProcesses();
            if (blockSchedule.IsBlockActive())
            {
                // Precompute list of blocked process names
                var blockedProcessNames = blockedApps.BlockedProcesses
                    .Where(p => !string.IsNullOrWhiteSpace(p.Executable))
                    .Select(p => p.Executable)
                    .ToList();

                // Single pass through processes
                foreach (var process in allProcesses)
                {
                    if (blockedProcessNames.Any(name => process.ProcessName.Contains(name)))
                    {
                        try
                        {
                            process.Kill();
                            Console.WriteLine("Process {0} (ID: {1}) terminated.", process.ProcessName, process.Id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error terminating process {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                    else
                    {
                        process.Dispose(); // Dispose even if not terminated
                    }
                }
            }
        }
    }
}


