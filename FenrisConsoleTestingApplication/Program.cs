
using static Fenris.BlockSettings;
using Fenris;
using Fenris.DiscoveryServices;



//var blockSettings = new BlockSettingsUrl();
//blockSettings.UrlBlock.Add("www.netflix.com", BlockType.Full);

////UserConfiguration.StoreBlockedWebsites(blockSettings);

//foreach (var block in blockSettings.UrlBlock)
//{
//    WebBlockerFirewall.AddFirewallBlock(block.Key, block.Value);
//}
await UserConfiguration.ClearWebsiteBlock();

Console.ReadKey();
