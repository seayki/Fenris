using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Fenris
{
    public class BlockSettingUrl
    {    
        public Dictionary<string, List<string>> UrlIps { get; set; }
        public BlockSettingUrl(Dictionary<string, List<string>> block)
        {
            UrlIps = block;
        }
    }
}
