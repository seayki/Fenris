using Fenris.DiscoveryServices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenrisService.BackgroundWorkers.IconUrl
{
    public static class IconFetcher
    {
        public static async Task RunIconUrlService()
        {
            var webBlock = await UserConfiguration.LoadBlockedWebsites();
            if (webBlock != null) 
            {
                foreach (var item in webBlock.UrlBlock)
                {
                    var icon = item.Value.IconBase64;
                    if (icon == null)
                    {
                        var favIcon = await FavIconService.GetFaviconFromUrl(item.Key);
                        if (favIcon == null)
                        {
                            item.Value.IconBase64 = "Empty";
                        }
                        else
                        {
                            item.Value.IconBase64 = ConvertIconToBase64(favIcon);
                        }
                    }
                }
                await UserConfiguration.StoreBlockedWebsites(webBlock, false);
            }       
        }
        private static string? ConvertIconToBase64(Icon icon)
        {
            if (icon == null)
            {
                return null;
            }
            using MemoryStream ms = new();
            icon.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
