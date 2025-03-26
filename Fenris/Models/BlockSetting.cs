using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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