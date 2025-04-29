using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris.Models
{
    public class Process
    {
        public string Name { get; set; }
        public string? IconUrl { get; set; }
        public string Executable { get; set; }

        public Process(string name, string? iconUrl, string executable)
        {
            Name = name;
            IconUrl = iconUrl;
            Executable = executable;
        }
    }
}
