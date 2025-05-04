using FenrisUI.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Windows.UI.Color;

namespace FenrisUI.Services
{
    public static class StaticDataService
    {
        public static List<string> WeekDays = new List<string>
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };

        public static List<WebsiteCategory> WebsiteCategory = new List<WebsiteCategory>
        {
             new WebsiteCategory("Social Media", new FontIcon { Glyph = "\uE8FA", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
             new WebsiteCategory("Gambling", new FontIcon { Glyph = "\uE707", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
             new WebsiteCategory("Streaming", new FontIcon { Glyph = "\uE768", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
             new WebsiteCategory("Adult Content", new FontIcon { Glyph = "\uE7EE", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
             new WebsiteCategory("News", new FontIcon { Glyph = "\uE789", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
             new WebsiteCategory("Shopping", new FontIcon { Glyph = "\uE719", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
             new WebsiteCategory("Gaming",  new FontIcon { Glyph = "\uE7FC", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") })
        };

        public static List<string> RecognizedDomains = new List<string>
        {
            "com", "net", "org", "gov", "edu", "int", "mil", "co", "info", "io", "biz", "us", "ca", "de", "uk", "fr", "au", "jp", "in", "cn", "ru", "br", "za", "mx", "es", 
            "it", "se", "nl", "pl", "ch", "kr", "fi", "no", "dk", "ie", "be", "at", "cz", "il", "gr", "hu", "ro", "sk", "bg", "lt", "lv", "ee", "si", "hr", "rs", "ua", "by", 
            "me", "mk", "ba", "ng", "pk", "ph", "tw", "ae", "sa", "hk", "sg", "my", "th", "vn", "bd", "ke", "ng", "nz", "pe", "cl", "ar", "ve", "co", "uy", "py", "ec", "bo", 
            "do", "gt", "sv", "hn", "ni", "cr", "pa", "jm", "tt", "bs", "bb", "lc", "gd", "kn", "vc", "dm", "tt", "ms", "sx", "mq", "gp", "re", "yt", "pf", "wf", "nc", "tv", 
            "as", "gu", "mp", "vi", "fm", "mh", "pw", "ki", "to", "ck", "fm", "nc", "tv", "pm", "ai", "bl", "gp", "mq", "re", "yt", "pf", "wf", "nc"
        };
        public static List<string> GetTopWebsites(string category)
        {
            return category switch
            {
                "Social Media" => new List<string>
                {
                "facebook.com", "twitter.com", "instagram.com", "tiktok.com", "snapchat.com",
                "linkedin.com", "reddit.com", "pinterest.com", "discord.com", "tumblr.com"
                },
                "Gambling" => new List<string>
                {
                "bet365.com", "pokerstars.com", "williamhill.com", "888casino.com", "betfair.com",
                "draftkings.com", "fanduel.com", "bovada.lv", "partycasino.com", "unibet.com"
                },
                "Streaming" => new List<string>
                {
                "youtube.com", "netflix.com", "hulu.com", "disneyplus.com", "amazonPrimeVideo.com",
                "hboMax.com", "twitch.tv", "peacocktv.com", "crunchyroll.com", "paramountplus.com"
                },
                "Adult Content" => new List<string>
                {
                "pornhub.com", "xvideos.com", "xnxx.com", "redtube.com", "youporn.com",
                "brazzers.com", "onlyfans.com", "fapello.com", "spankbang.com", "hclips.com"
                },
                "News" => new List<string>
                {
                "cnn.com", "bbc.com", "nytimes.com", "theguardian.com", "foxnews.com",
                "washingtonpost.com", "npr.org", "aljazeera.com", "reuters.com", "bloomberg.com"
                },
                "Shopping" => new List<string>
                {
                "amazon.com", "ebay.com", "walmart.com", "aliexpress.com", "etsy.com",
                "target.com", "bestbuy.com", "costco.com", "wayfair.com", "newegg.com"
                },
                "Gaming" => new List<string>
                {
                "steampowered.com", "epicgames.com", "playstation.com", "xbox.com", "nintendo.com",
                "twitch.tv", "ign.com", "gamespot.com", "rockstargames.com", "riotgames.com"
                },
                _ => new List<string>()
            };
        }

        public static Button GetAddTimeButton(string day)
        {
            var addTimeButton = new Button
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 85, 102, 255)),
                Content = "Add",
                Width = 100,
                CornerRadius = new CornerRadius(5),
                Tag = day,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            return addTimeButton;
        }
    }
}
