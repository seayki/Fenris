using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Fenris
{
    public class BlockSettingsUrl
    {
        public Dictionary<string, BlockType> UrlBlock { get; set; } = new();
        public BlockSettingsUrl(string url, BlockType type)
        {
            UrlBlock.Add(url, type);
        }
        public BlockSettingsUrl()
        {
            
        }

        public void AddEntry(string url, BlockType type)
        {
            UrlBlock.Add(url, type);
        }
    }
}
