
using static Fenris.BlockSettings;
using Fenris;
using Fenris.DiscoveryServices;

//var steamdiscoveryservice = new steamdiscoveryservice();
//var applicationdiscoveryservice = new applicationdiscoveryservice();
//var discoveryservice = new discoveryservice(steamdiscoveryservice, applicationdiscoveryservice);
//list<process> processes = discoveryservice.discovergames();

var webBlocker = new WebBlocker();
var urlsToBlock = new List<string>
{
    "https://www.twitter.com"
};
webBlocker.AddBlock(urlsToBlock);
var iconsAndUrls = await webBlocker.GetBlockedUrlAndIcon();
Console.ReadKey();
