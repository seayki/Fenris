using Fenris;
using Fenris.DiscoveryServices;
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
            BlockSettings settings = await UserConfiguration.LoadBlockSettings();
            if (settings == null) return;

            foreach (var process in settings.BlockedProcesses)
            {
                if (settings.IsBlockActive(process))
                {
                    try
                    {
                        var processesToTerminate = System.Diagnostics.Process.GetProcessesByName(process.Name);
                        if (processesToTerminate.Length > 0)
                        {
                            foreach (var proc in processesToTerminate)
                            {
                                proc.Kill();
                                Console.WriteLine($"Terminated process: {proc.ProcessName}"); // Consider ILogger here
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


