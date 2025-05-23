

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
