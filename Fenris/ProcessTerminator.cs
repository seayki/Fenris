using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris
{
    public class ProcessTerminator
    {
        List<Process> Processes;
        public ProcessTerminator(List<Process> processes)
        {
            Processes = processes;
        }

        public List<Process> RetrieveProcessesToTerminate()
        {
            var blockedProcesses = Processes.Where(a => a.HasBlock == true).ToList();
            return blockedProcesses;
        }

        public void RunTerminateProcessBackgroundService()
        {
            var blockedProcesses = RetrieveProcessesToTerminate();
            while (true)
            {
                foreach (var process in blockedProcesses)
                {
                    if (process.BlockSettings!.IsBlockActive())
                    {
                        var processesToTerminate = System.Diagnostics.Process.GetProcessesByName("BackgroundService");
                        foreach (var processToTerminate in processesToTerminate)
                        {
                            try
                            {
                                processToTerminate.Kill();
                                Console.WriteLine($"Terminated process: {processToTerminate.ProcessName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to terminate {processToTerminate.ProcessName}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(process.Name + "is not blocked at the current time");
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
