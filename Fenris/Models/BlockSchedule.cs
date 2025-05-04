using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris.Models
{
    public class BlockSchedule
    {
        public Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> Block { get; set; } = new();

        public BlockSchedule()
        {
            
        }
        public BlockSchedule(Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>> block)
        {
            Block = block;
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
}
