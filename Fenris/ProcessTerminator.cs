using Fenris.DiscoveryServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris
{

    public class ProcessTerminator : IProcessTerminator
    {
        private readonly IUserConfiguration _userConfiguration;
        public ProcessTerminator(IUserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
        }

        public List<Process> RetrieveProcessesToTerminate()
        {
            // Read processes from JSON or something else, and add to list if has a block.
            //var blockedProcesses = Processes.Where(a => a.HasBlock == true).ToList();
            var blockedProcesses = new List<Process>();
            return blockedProcesses;
        }

        public async void RunTerminateProcess()
        {
            BlockSettings settings = _userConfiguration.LoadBlockSettings();
            if (settings == null) return; // No settings to enforce yet

            await Task.Run(async () =>
            {
                while (true)
                {
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
                    await Task.Delay(60000); // Check every minute
                }
            });
        }
    }
}


