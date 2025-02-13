using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fenris.BlockSettings;

namespace Fenris
{
    public class Process
    {
        public string Name { get; set; }
        public string IconUrl { get; set; }
        public string InstallPath { get; set; }
        public string Executable { get; set; }
        public bool HasBlock { get; set; }  
        public BlockSettings? BlockSettings { get; set; }

        public Process(string name, string iconUrl, string installPath, string executable, bool hasBlock = false)
        {
            Name = name;
            IconUrl = iconUrl;
            InstallPath = installPath;
            Executable = executable;
            HasBlock = hasBlock;
        }

        public void AddBlock(BlockDefaults blockDefaults)
        {
            BlockSettings = new BlockSettings(blockDefaults);
            HasBlock = true;
        }

        public void RemoveBlock()
        {
            BlockSettings = null;
            HasBlock = false;          
        }   
    }

    public class BlockSettings
    {
        public BlockSettings(BlockDefaults blockDefaults)
        {
            CreateNewBlock(BlockDefaults.Default);
        }
        public bool IsBlocked { get; set; } = false;
        public TimeSpan BlockStart { get; set; }
        public TimeSpan BlockEnd { get; set; }
        public List<DayOfWeek> DaysBlocked { get; set; } = new();

        public bool IsBlockActive()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentDay = DateTime.Now.DayOfWeek;

            if (DaysBlocked.Contains(currentDay) && currentTime >= BlockStart && currentTime <= BlockEnd)
            {
                return true;
            }
            return false;
        }

        public void CreateNewBlock(BlockDefaults blockDefaults)
        {
            if (blockDefaults == BlockDefaults.Default)
            {
                IsBlocked = true;
                BlockStart = new TimeSpan(8, 0, 0);
                BlockEnd = new TimeSpan(17, 0, 0); 
                DaysBlocked.AddRange(new List<DayOfWeek>
                { 
                    DayOfWeek.Monday, 
                    DayOfWeek.Tuesday, 
                    DayOfWeek.Wednesday, 
                    DayOfWeek.Thursday, 
                    DayOfWeek.Friday
                });
            }
        }

        public enum BlockDefaults
        {
            Default, // 8-4, weekends off
            Custom,  // custom rule set
        }
    }
}
