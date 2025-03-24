using ABI.Windows.ApplicationModel.Activation;
using Fenris;
using Fenris.DiscoveryServices;
using FenrisUI.Models;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;
using Color = Windows.UI.Color;
using Process = Fenris.Process;

namespace FenrisUI
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public List<Process> FilteredProcesses { get; set; } = new();
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();
        private HashSet<string> selectedDays = new HashSet<string>();
        private Dictionary<string, List<TimePicker>> dayTimePickers = new Dictionary<string, List<TimePicker>>();
        
        public ObservableCollection<WebsiteCategory> Labels { get; set; } = new ObservableCollection<WebsiteCategory>();
        public ObservableCollection<UrlBlock> urlBlocks { get; set; } = new ObservableCollection<UrlBlock>();

        private readonly IDiscoveryService _discoveryService;
        private BlockSettings BlockSettings { get; set; }

        private ObservableCollection<Process> _processes = new();
        public ObservableCollection<Process> SelectedProcesses { get; } = new();
        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set
            {
                _processes = value;
                OnPropertyChanged(nameof(Processes));
            }
        }

        private bool _isBlocked = false;
        public bool IsBlocked
        {
            get => _isBlocked;
            set
            {
                if (_isBlocked != value)
                {
                    _isBlocked = value;
                    OnPropertyChanged(nameof(IsBlocked));
                }
            }
        }

        private SolidColorBrush _blockStatusColor = new SolidColorBrush(Colors.Red);
        public SolidColorBrush BlockStatusColor
        {
            get => _blockStatusColor;
            private set
            {
                _blockStatusColor = value;
                OnPropertyChanged(nameof(BlockStatusColor));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow(IDiscoveryService discoveryService)
        {
            _discoveryService = discoveryService;
            // Initialize time pickers dictionary
            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            {
                dayTimePickers[day] = new List<TimePicker>();
            }

            // Initialize web labels with icons
            foreach(var item in new[]
            {
                new WebsiteCategory("Social Media", new FontIcon { Glyph = "\uE8FA", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
                new WebsiteCategory("Gambling", new FontIcon { Glyph = "\uE707", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
                new WebsiteCategory("Streaming", new FontIcon { Glyph = "\uE768", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
                new WebsiteCategory("Adult Content", new FontIcon { Glyph = "\uE7EE", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
                new WebsiteCategory("News", new FontIcon { Glyph = "\uE789", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
                new WebsiteCategory("Shopping", new FontIcon { Glyph = "\uE719", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") }),
                new WebsiteCategory("Gaming",  new FontIcon { Glyph = "\uE7FC", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") })
            })
            {
                Labels.Add(item);
            }


            // Show loading indicator initially
            this.Content = new ProgressRing
            {
                IsActive = true,
                Width = 50,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Start loading data asynchronously
            _ = LoadDataAndInitializeAsync();
        }

        private async Task LoadDataAndInitializeAsync()
        {
            try
            {
                // Retrieve stored app and web block settings
                var blockSetting = await UserConfiguration.LoadBlockSettings();
                var blockSettingUrl = await UserConfiguration.LoadBlockedWebsites();
                var discoveredProcesses = await _discoveryService.DiscoverGames();
                

                this.Content = null;
                this.InitializeComponent();
                await Task.Delay(1000); // Wait for UI to initialize

                // Update UI to reflect a users block setting if one exists.
                if (discoveredProcesses != null)
                {
                    Processes.Clear();
                    foreach (var process in discoveredProcesses)
                    {
                        Processes.Add(process);
                    }
                }
                if (blockSetting != null)
                {
                    foreach (var process in blockSetting.BlockedProcesses)
                    {
                        int index = Processes.IndexOf(Processes.Where(a => a.Name == process.Name).First());
                        if (index >= 0)
                        {
                            var listViewItem = ProcessList.ContainerFromIndex(index) as ListViewItem;
                            if (listViewItem != null)
                            {
                                var stackPanel = listViewItem.ContentTemplateRoot as StackPanel;
                                if (stackPanel != null)
                                {
                                    stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 202, 233, 240));
                                    listViewItem.UpdateLayout();
                                    SelectedProcesses.Add(process);
                                }
                            }
                        }
                    }
                    foreach (var item in blockSetting.Block)
                    {
                        string day = item.Key.ToString();
                        StackPanel timePickersPanel = GetTimePickersPanel(day);
                        if (timePickersPanel != null)
                        {
                            selectedDays.Add(day);

                            // Find the corresponding day button
                            Button dayButton = null;
                            foreach (var child in DayWrapPanel.Children)
                            {
                                if (child is StackPanel sp && sp.Children[0] is Button btn && btn.Tag.ToString() == day)
                                {
                                    dayButton = btn;
                                    break;
                                }
                            }

                            if (dayButton != null)
                            {
                                dayButton.Opacity = 1;
                            }

                            timePickersPanel.Visibility = Visibility.Visible;
                            timePickersPanel.Children.Clear();
                            dayTimePickers[day].Clear();

                            // Add time boxes for each time range
                            foreach (var timeRange in item.Value)
                            {
                                AddTimeBox(day, timePickersPanel, timeRange.BlockStart, timeRange.BlockEnd);
                            }

                            var addTimeButton = new Button
                            {
                                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                                Background = new SolidColorBrush(Color.FromArgb(255, 46, 124, 203)),
                                Content = "Add Time",
                                Width = 100,
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(12, 6, 12, 6),
                                Tag = day,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Opacity = 0.9,
                                FontWeight = FontWeights.Bold,

                            };
                            addTimeButton.Click += AddTimeButton_Click;
                            timePickersPanel.Children.Add(addTimeButton);
                        }
                    }
                }
                else
                {
                    IsBlocked = false;
                    BlockStatusColor = new SolidColorBrush(Colors.Red);
                }
                if (blockSettingUrl != null)
                {
                    foreach (var item in blockSettingUrl.UrlBlock)
                    {
                        var imageIcon = await GetIcon(item.Value.IconBase64);
                        UrlBlock urlBlock = new UrlBlock(imageIcon, item.Key, item.Value.Type);
                        urlBlocks.Add(urlBlock);
                    }
                }
                _ = UpdateBlockStatusLoop();
            }
            catch (Exception ex)
            {
                this.Content = new TextBlock
                {
                    Text = $"Error loading data: {ex.Message}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async Task UpdateBlockStatusLoop()
        {
            while (true) // Runs on UI thread, stops when window closes
            {
                try
                {
                    BlockSettings = await UserConfiguration.LoadBlockSettings();
                    bool isBlocked = BlockSettings?.IsBlockActive() ?? false;

                    IsBlocked = isBlocked;
                    BlockStatusColor = IsBlocked ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Update loop error: {ex.Message}");
                }
                await Task.Delay(5000);
            }
        }

        private async void UpdateBlockTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button toggleButton)
            {
                if (toggleButton.DataContext is UrlBlock urlBlock)
                {
                    if (urlBlock.BlockType == BlockType.Full)
                    {
                        urlBlock.BlockType = BlockType.Schedule;
                        toggleButton.Content = "Schedule";
                        toggleButton.Background = urlBlock.BackgroundColor;
                    }
                    else if (urlBlock.BlockType == BlockType.Schedule)
                    {
                        urlBlock.BlockType = BlockType.Full;
                        toggleButton.Content = "Full";
                        toggleButton.Background = urlBlock.BackgroundColor;
                    }
                    // Update firewall in json
                    await UserConfiguration.UpdateWebsiteBlockType(urlBlock.Url, urlBlock.BlockType);
                }
            }
        }

        private void MainScrollViewer_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - delta);
                e.Handled = true;
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e) => AddTag();

        private void TagInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddTag();
                e.Handled = true;
            }
        }

        private async void RemoveWebBlock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var url = button.Tag.ToString();
                await UserConfiguration.RemoveWebsiteBlock(url);
                WebBlockerFirewall.RemoveFirewallBlock(url);
                urlBlocks.Remove(urlBlocks.Where(a => a.Url == url).First());
            }
        }

        private void AddTag()
        {
            string newTag = TagInput.Text.Trim();
            if (!string.IsNullOrEmpty(newTag) && !Tags.Contains(newTag))
            {
                var processesToFilter = Processes.Where(p => p.Name.ToLower().Contains(newTag.ToLower())).ToList();
                processesToFilter.ForEach(p => Processes.Remove(p));
                FilteredProcesses.AddRange(processesToFilter);

                Tags.Add(newTag);
                TagInput.Text = "";
            }
        }

        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                var processesToUnfilter = FilteredProcesses.Where(p => p.Name.Contains(tag)).ToList();
                processesToUnfilter.ForEach(p => FilteredProcesses.Remove(p));
                processesToUnfilter.ForEach(p => Processes.Add(p));

                Tags.Remove(tag);
            }
        }

        private void ProcessList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Process process)
            {
                var listViewItem = ProcessList.ContainerFromItem(process) as ListViewItem;
                if (listViewItem != null)
                {
                    var stackPanel = listViewItem.ContentTemplateRoot as StackPanel;
                    if (stackPanel != null)
                    {
                        if (SelectedProcesses.Contains(process))
                        {
                            SelectedProcesses.Remove(process);
                            stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        }
                        else
                        {
                            SelectedProcesses.Add(process);
                            stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 202, 233, 240));
                        }
                    }
                }
            }
        }

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string day = button.Tag.ToString();
                StackPanel timePickersPanel = GetTimePickersPanel(day);

                if (timePickersPanel == null) return;

                if (selectedDays.Contains(day))
                {
                    selectedDays.Remove(day);
                    button.Opacity = 0.8;
                    timePickersPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    selectedDays.Add(day);
                    button.Opacity = 1;
                    timePickersPanel.Visibility = Visibility.Visible;

                    timePickersPanel.Children.Clear();
                    dayTimePickers[day].Clear();

                    AddTimeBox(day, timePickersPanel);

                    var addTimeButton = new Button
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                        Background = new SolidColorBrush(Color.FromArgb(255, 46, 124, 203)),
                        Content = "Add Time",
                        Width = 100,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 6, 12, 6),
                        Tag = day,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Opacity = 0.9,
                        FontWeight = FontWeights.Bold,

                    };
                    addTimeButton.Click += AddTimeButton_Click;
                    timePickersPanel.Children.Add(addTimeButton);
                }
            }
        }

        private StackPanel GetTimePickersPanel(string day)
        {
            return day switch
            {
                "Monday" => this.MondayTimePickers,
                "Tuesday" => this.TuesdayTimePickers,
                "Wednesday" => this.WednesdayTimePickers,
                "Thursday" => this.ThursdayTimePickers,
                "Friday" => this.FridayTimePickers,
                "Saturday" => this.SaturdayTimePickers,
                "Sunday" => this.SundayTimePickers,
                _ => null
            };
        }

        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string day = button.Tag.ToString();
                StackPanel timePickersPanel = GetTimePickersPanel(day);
                if (timePickersPanel != null)
                {
                    AddTimeBox(day, timePickersPanel);
                }
            }
        }

        private void AddTimeBox(string day, StackPanel panel, TimeSpan? from = null, TimeSpan? to = null)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 236, 238, 236)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Width = 270,
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 10
            };

            var fromTimePicker = new TimePicker
            {
                Header = "From",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                ClockIdentifier = "24HourClock",
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                SelectedTime = from.HasValue ? from.Value : new TimeSpan(8, 0, 0),
                FontWeight = FontWeights.Bold
            };

            var toTimePicker = new TimePicker
            {
                Header = "To",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                ClockIdentifier = "24HourClock",
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                SelectedTime = to.HasValue ? to.Value : new TimeSpan(15, 0, 0),
                FontWeight = FontWeights.Bold
            };

            var deleteButton = new Button
            {
                Content = "Delete",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                Background = new SolidColorBrush(Color.FromArgb(255, 250, 109, 122)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(1.4),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            dayTimePickers[day].Add(fromTimePicker);
            dayTimePickers[day].Add(toTimePicker);

            deleteButton.Click += (s, e) =>
            {
                panel.Children.Remove(border);
                dayTimePickers[day].Remove(fromTimePicker);
                dayTimePickers[day].Remove(toTimePicker);
            };

            stackPanel.Children.Add(fromTimePicker);
            stackPanel.Children.Add(toTimePicker);
            stackPanel.Children.Add(deleteButton);

            border.Child = stackPanel;

            if (panel.Children.Count > 0)
            {
                panel.Children.Insert(panel.Children.Count - 1, border);
            }
            else
            {
                panel.Children.Add(border);
            }
        }
        
        private void ApplyBlock_Click(object sender, RoutedEventArgs e)
        {
            var processesToBlock = SelectedProcesses.ToList();
            var blockTimes = new Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>>();

            foreach (var day in selectedDays)
            {
                if (dayTimePickers.ContainsKey(day))
                {
                    blockTimes[Enum.Parse<DayOfWeek>(day)] = new List<(TimeSpan BlockStart, TimeSpan BlockEnd)>();
                    for (int i = 0; i < dayTimePickers[day].Count; i += 2)
                    {
                        var fromTime = dayTimePickers[day][i].Time;
                        var toTime = dayTimePickers[day][i + 1].Time;
                        blockTimes[Enum.Parse<DayOfWeek>(day)].Add((fromTime, toTime));
                    }
                }
            }
            var blockSettings = new BlockSettings(processesToBlock, blockTimes);
            UserConfiguration.StoreBlockSettings(blockSettings);
        }

        public async Task CreateUrlBlock(string url, bool bulk = false)
        {
            UrlBlock urlBlock = new UrlBlock(null, url, BlockType.Full);
            urlBlocks.Add(urlBlock);
            WebBlockerFirewall.AddFirewallBlock(url, BlockType.Full);
            if (!bulk) 
            {
                await UserConfiguration.StoreBlockedWebsites(new BlockSettingsUrl(url, BlockType.Full));
            }
        }

        public async void ApplyBlockUrl_Click(object sender, RoutedEventArgs e)
        {
            string url = WebsiteTextBox.Text.Trim();
            if (string.IsNullOrEmpty(url))
                WebsiteTextBox.Text = "";
            url = CheckAndFormatUrl(url);
            await CreateUrlBlock(url);
        }

        public async void ApplyLabelBlock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var label = button.Tag.ToString();
                var websites = GetTopWebsites(label);
                var urlBlock = new BlockSettingsUrl();
                var tasks = new List<Task>();  
                foreach (var website in websites)
                {
                    var websiteFormatted = CheckAndFormatUrl(website);
                    tasks.Add(CreateUrlBlock(websiteFormatted, true));
                    urlBlock.UrlBlock.Add(websiteFormatted, new BlockData(BlockType.Full));
                }
                await Task.WhenAll(tasks);
                await UserConfiguration.StoreBlockedWebsites(urlBlock);
            }        
        }

        public string CheckAndFormatUrl(string url)
        {
            url = url.Trim().ToLower();

            // Check if the URL starts with "www."
            if (!url.StartsWith("www."))
            {
                // If not, add "www." to the beginning of the URL
                url = "www." + url;
            }

            // Split to validate domain format (e.g., "example.com")
            var parts = url.Split('.');
            if (parts.Length < 2 || parts.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Invalid URL format. A domain and top-level domain are required.", nameof(url));
            }

            return url;
        }

        private List<string> GetTopWebsites(string category)
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



        public static async Task<SoftwareBitmapSource> IconToImageSourceAsync(Icon icon)
        {
            if (icon == null) return null;

            using (var stream = new MemoryStream())
            {
                icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);

                var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap,
                                                   BitmapPixelFormat.Bgra8,
                                                   BitmapAlphaMode.Premultiplied);
                var imageSource = new SoftwareBitmapSource();
                await imageSource.SetBitmapAsync(softwareBitmap);
                return imageSource;
            }
        }

        public async Task<SoftwareBitmapSource?> GetIcon(string? iconString)
        {
            if (string.IsNullOrEmpty(iconString) || iconString == "Empty")
                return null;

            try
            {
                byte[] bytes = Convert.FromBase64String(iconString);
                using MemoryStream ms = new(bytes);
                using Icon icon = new(ms);

                return await IconToImageSourceAsync(icon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decoding icon: {ex.Message}");
                return null;
            }
        }
    }
}