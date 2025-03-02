using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public interface IUserConfiguration
    {
        public BlockSettings LoadBlockSettings();
        public void StoreBlockSettings(BlockSettings settings);
        public List<string> LoadBlockedWebsites();
        public void StoreBlockedWebsites(List<string> websites);
    }
}
