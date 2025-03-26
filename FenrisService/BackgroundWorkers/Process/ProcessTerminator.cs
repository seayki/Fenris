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

namespace FenrisService.BackgroundWorkers.Process
{

    public static class ProcessTerminator
    {
        public static async Task RunTerminateProcess()
        {
            var blockedApps = await UserConfiguration.LoadBlockedApps();
            var blockSchedule = await UserConfiguration.LoadBlockSchedule();
            if (blockedApps == null || blockSchedule == null) return;

            if (blockSchedule.IsBlockActive())
            {
                foreach (var process in blockedApps.BlockedProcesses)
                {
                    try
                    {
                        var processesToTerminate = System.Diagnostics.Process.GetProcessesByName(process.Name);
                        if (processesToTerminate.Length > 0)
                        {
                            foreach (var proc in processesToTerminate)
                            {
                                proc.Kill();
                                Console.WriteLine($"Terminated process: {proc.ProcessName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to terminate process {process.Name}: {ex.Message}");
                    }
                }
            }
        }
    }
}


