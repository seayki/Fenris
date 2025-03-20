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
        public Dictionary<string, BlockData> UrlBlock { get; set; } = new();
        public BlockSettingsUrl(string url, BlockType type, Icon icon)
        {
            UrlBlock.Add(url, new BlockData(type, icon));
        }
        public BlockSettingsUrl()
        {
            
        }
    }

    public class BlockData
    {
        public BlockType Type { get; set; }
        public string? IconBase64 { get; set; }

        public BlockData(BlockType type, Icon icon)
        {
            Type = type;
            IconBase64 = ConvertIconToBase64(icon);
        }

        public BlockData()
        {
        }

        private static string? ConvertIconToBase64(Icon icon)
        {
            if (icon == null) {
                return null;
            }
            using MemoryStream ms = new();
            icon.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
