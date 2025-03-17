    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fenris
{
    public class BlockSettings
    {
        public List<Process> BlockedProcesses { get; set; } = new();
        public Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> Block { get; set; } = new();

        public BlockSettings(List<Process> processes, Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> block)
        {
            BlockedProcesses = processes;
            Block = block;
        }

        public BlockSettings()
        {
            
        }

        public bool IsBlockActive(Process process)
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentDay = DateTime.Now.DayOfWeek;

            if (Block.ContainsKey(currentDay) && BlockedProcesses.Contains(process))
            {
                foreach (var (blockStart, blockEnd) in Block[currentDay])
                {
                    if (currentTime >= blockStart && currentTime <= blockEnd)
                    {
                        return true; 
                    }
                }
            }
            return false; 
        }

        public bool IsBlockActive()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentDay = DateTime.Now.DayOfWeek;

            if (Block.ContainsKey(currentDay))
            {
                foreach (var (blockStart, blockEnd) in Block[currentDay])
                {
                    if (currentTime >= blockStart && currentTime <= blockEnd)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public enum BlockType
    {
        Full,
        Schedule,
        None
    }
}