

namespace Fenris.Models
{
    public class BlockSettings
    {
        public List<Process> BlockedProcesses { get; set; } = new();

        public BlockSettings(List<Process> processes)
        {
            BlockedProcesses = processes;
        }

        public BlockSettings()
        {
            
        }
    }
}