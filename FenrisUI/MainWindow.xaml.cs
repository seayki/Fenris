using Fenris.DiscoveryServices;
using Fenris.Models;
using Fenris.Storage;
using FenrisUI.Models;
using FenrisUI.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Color = Windows.UI.Color;
using IconService = FenrisUI.Services.IconService;
using Process = Fenris.Models.Process;

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

        public DiscoveryService DiscoveryService = new();

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

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize time pickers dictionary
                foreach (var day in StaticDataService.WeekDays)
                {
                    dayTimePickers[day] = new List<TimePicker>();
                }

                // Initialize web labels with icons
                foreach (var category in StaticDataService.WebsiteCategory)
                {
                    Labels.Add(category);
                }

                await LoadDataAsync();

                LoadingGrid.Visibility = Visibility.Collapsed;
                AppContentGrid.Visibility = Visibility.Visible;
            }
            catch
            {
                RootGrid.Children.Clear();
                RootGrid.Children.Add(new TextBlock
                {
                    Text = "Error loading data.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
        }


        #region General
        private async Task LoadDataAsync()
        {
            var discoveredProcesses = await DiscoveryService.DiscoverProcesses();
            var blockSetting = await UserConfiguration.LoadBlockedApps();
            var blockSettingUrl = await UserConfiguration.LoadBlockedWebsites();
            var blockSchedule = await UserConfiguration.LoadBlockSchedule();
         
            LoadDiscoveredProcesses(discoveredProcesses);

            if (blockSettingUrl != null)
            {
                await LoadBlockedUrls(blockSettingUrl);
            }
            if (blockSetting != null)
            {
                ApplyAppBlockSettings(blockSetting);
            }
            if (blockSchedule != null)
            {
                ApplyScheduleBlockSettings(blockSchedule);
            }
            _ = UpdateBlockStatusLoop();
        }


        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        #endregion

        #region Apps
        private void ApplyAppBlockSettings(BlockSettings blockSetting)
        {
            if (blockSetting == null) return;

            var sorted = Processes
            .OrderByDescending(p => blockSetting.BlockedProcesses.Any(bp => bp.Name == p.Name))
            .ToList();

            Processes.Clear();
            foreach (var process in sorted)
            {
                Processes.Add(process);
            }

            foreach (var process in blockSetting.BlockedProcesses)
            {
                var name = Processes.FirstOrDefault(a => a.Name == process.Name);
                if (name == null) continue;
                // Potentially remove process if not found but exists in blockSetting
                var index = Processes.IndexOf(name);

                ProcessList.ScrollIntoView(Processes[index]);

                if (ProcessList.ContainerFromIndex(index) is ListViewItem listViewItem &&
                    listViewItem.ContentTemplateRoot is StackPanel stackPanel)
                {
                    stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 192, 200, 209));
                    stackPanel.Opacity = 0.9;
                    listViewItem.UpdateLayout();
                    SelectedProcesses.Add(process);
                }
            }
        }

        private void LoadDiscoveredProcesses(List<Process> discoveredProcesses)
        {
            if (discoveredProcesses == null) return;

            Processes.Clear();
            foreach (var process in discoveredProcesses)
            {
                Processes.Add(process);
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e) => AddTag();
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
                        var existingProcess = SelectedProcesses.FirstOrDefault(a => a.Name == process.Name);
                        if (existingProcess != null)
                        {
                            // Unselect
                            SelectedProcesses.Remove(existingProcess);
                            stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 228, 229, 231));
                            stackPanel.Opacity = 1;
                        }
                        else
                        {
                            // Select
                            SelectedProcesses.Add(process);
                            stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 192, 200, 209));
                            stackPanel.Opacity = 0.9;
                        }
                    }
                }
            }
        }
        public async void UpdateApps_Click(object sender, RoutedEventArgs e)
        {
            var blockedProcesses = await UserConfiguration.LoadBlockedApps();
            if (SelectedProcesses != null)
            {
                if (blockedProcesses != null && IsBlocked)
                {
                    var blockedProcessesNames = blockedProcesses.BlockedProcesses
                        .Select(p => p.Name)
                        .ToHashSet();
                    var selectedProcessesNames = SelectedProcesses
                        .Select(p => p.Name)
                        .ToHashSet();
                    bool allBlockedProcessesSelected = blockedProcessesNames.All(name => selectedProcessesNames.Contains(name));

                    if (!allBlockedProcessesSelected)
                    {
                        await ShowBlockMessage();
                        return;
                    }
                }
                var processesToBlock = SelectedProcesses.ToList();
                var blockSettings = new BlockSettings(processesToBlock);
                await UserConfiguration.StoreBlockedApps(blockSettings);
                ShowInfoMessage("Saved Changes", InfoEnum.success);
            }
        }
        #endregion

        #region Schedule
        private void ApplyScheduleBlockSettings(BlockSchedule blockSchedule)
        {
            if (blockSchedule == null)
            {
                IsBlocked = false;
                BlockStatusColor = new SolidColorBrush(Colors.Red);

            }
            foreach (var item in blockSchedule!.Block)
            {
                string day = item.Key.ToString();
                StackPanel timePickersPanel = GetTimePickersPanel(day);
                if (timePickersPanel == null) continue;

                selectedDays.Add(day);
                Button dayButton = FindDayButton(day);
                if (dayButton != null)
                {
                    dayButton.Opacity = 1;
                }

                timePickersPanel.Visibility = Visibility.Visible;
                timePickersPanel.Children.Clear();
                dayTimePickers[day].Clear();

                foreach (var timeRange in item.Value)
                {
                    AddTimeBox(day, timePickersPanel, timeRange.BlockStart, timeRange.BlockEnd);
                }
                var addTimeButton = StaticDataService.GetAddTimeButton(day);
                addTimeButton.Click += AddTimeButton_Click;
                timePickersPanel.Children.Add(addTimeButton);
            }
        }

        private Button FindDayButton(string day)
        {
            foreach (var child in DayWrapPanel.Children)
            {
                if (child is StackPanel sp && sp.Children[0] is Button btn && btn.Tag?.ToString() == day)
                {
                    return btn;
                }
            }
            throw new Exception($"Button for {day} not found.");
        }

        private async Task UpdateBlockStatusLoop()
        {
            while (true) // Runs on UI thread, stops when window closes
            {
                try
                {
                    var blockSchedule = await UserConfiguration.LoadBlockSchedule();
                    bool isBlocked = blockSchedule?.IsBlockActive() ?? false;

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

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string day = button.Tag.ToString()!;
                StackPanel timePickersPanel = GetTimePickersPanel(day);

                if (timePickersPanel == null) return;

                if (selectedDays.Contains(day))
                {
                    selectedDays.Remove(day);
                    button.Opacity = 0.6;
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

                    var buttonToAdd = StaticDataService.GetAddTimeButton(day);
                    buttonToAdd.Click += AddTimeButton_Click;
                    timePickersPanel.Children.Add(buttonToAdd);
                }
            }
        }

        private StackPanel GetTimePickersPanel(string day)
        {
            if (!Enum.TryParse<DayOfWeek>(day, out var dayEnum))
                throw new ArgumentException($"Invalid day: {day}", nameof(day));

            return dayEnum switch
            {
                DayOfWeek.Monday => MondayTimePickers,
                DayOfWeek.Tuesday => TuesdayTimePickers,
                DayOfWeek.Wednesday => WednesdayTimePickers,
                DayOfWeek.Thursday => ThursdayTimePickers,
                DayOfWeek.Friday => FridayTimePickers,
                DayOfWeek.Saturday => SaturdayTimePickers,
                DayOfWeek.Sunday => SundayTimePickers,
                _ => throw new ArgumentOutOfRangeException(nameof(dayEnum), dayEnum, $"Day: {dayEnum} was not found.")
            };
        }
        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string day = button.Tag.ToString()!;
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
                Background = new SolidColorBrush(Color.FromArgb(255, 245, 246, 248)), // #F5F6F8
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Width = 250,
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 10
            };

            var fromTimePicker = new TimePicker
            {
                Header = "From",
                ClockIdentifier = "24HourClock",
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), // White
                SelectedTime = from.HasValue ? from.Value : new TimeSpan(8, 0, 0),
                FontWeight = FontWeights.Bold
            };

            var toTimePicker = new TimePicker
            {
                Header = "To",
                ClockIdentifier = "24HourClock",
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), // White
                SelectedTime = to.HasValue ? to.Value : new TimeSpan(15, 0, 0),
                FontWeight = FontWeights.Bold
            };

            var deleteButton = new Button
            {
                Content = "Remove",
                Background = new SolidColorBrush(Color.FromArgb(255, 185, 61, 55)),
                HorizontalAlignment = HorizontalAlignment.Right,
                CornerRadius = new CornerRadius(5)
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
        public async void UpdateSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (IsBlocked)
            {
                await ShowBlockMessage();
                return;
            }
            var blockTimes = new Dictionary<DayOfWeek, List<(TimeSpan BlockStart, TimeSpan BlockEnd)>>();
            var now = DateTime.Now;
            var currentDay = now.DayOfWeek;
            var currentTime = now.TimeOfDay;
            bool affectsCurrentTime = false;

            foreach (var day in selectedDays)
            {
                if (dayTimePickers.ContainsKey(day))
                {
                    var dayEnum = Enum.Parse<DayOfWeek>(day);
                    blockTimes[dayEnum] = new List<(TimeSpan BlockStart, TimeSpan BlockEnd)>();

                    for (int i = 0; i < dayTimePickers[day].Count; i += 2)
                    {
                        var fromTime = dayTimePickers[day][i].Time;
                        var toTime = dayTimePickers[day][i + 1].Time;

                        if (dayEnum == currentDay && fromTime <= currentTime && currentTime <= toTime)
                        {
                            affectsCurrentTime = true;
                        }

                        blockTimes[dayEnum].Add((fromTime, toTime));
                    }
                }
            }
            if (affectsCurrentTime)
            {
                bool userConfirmed = await ShowConfirmationDialog("The new block schedule includes the current time. You won't be able to modify any of the blocks during active periods. Do you want to proceed?");
                if (!userConfirmed)
                {
                    return; // Stop execution if user cancels
                }
            }
            await UserConfiguration.StoreBlockSchedule(new BlockSchedule(blockTimes));
            ShowInfoMessage("Saved Changes", InfoEnum.success);
        }

        #endregion

        #region Website Block

        private void AddWebsite_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddUrlBlock_Click(sender, e);
            }
        }
        private async Task LoadBlockedUrls(BlockSettingsUrl blockSettingUrl)
        {
            if (blockSettingUrl == null) return;

            foreach (var item in blockSettingUrl.UrlBlock)
            {
                var imageIcon = await IconService.GetIcon(item.Value.IconBase64);
                UrlBlock urlBlock = new UrlBlock(imageIcon, item.Key, item.Value.Type);
                urlBlocks.Add(urlBlock);
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
                        toggleButton.Content = BlockType.Schedule;
                        toggleButton.Background = urlBlock.BackgroundColor;
                    }
                    else if (urlBlock.BlockType == BlockType.Schedule)
                    {
                        urlBlock.BlockType = BlockType.Full;
                        toggleButton.Content = BlockType.Full;
                        toggleButton.Background = urlBlock.BackgroundColor;
                    }
                    // Update block type in json
                    await UserConfiguration.UpdateWebsiteBlockType(urlBlock.Url, urlBlock.BlockType);
                }
            }
        }

        private async void RemoveWebBlock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (IsBlocked)
                {
                    await ShowBlockMessage();
                    return;
                }
                string url = button.Tag.ToString()!;
                bool doesExist = urlBlocks.Where(a => a.Url == url).Any();
                if (doesExist)
                {
                    urlBlocks.Remove(urlBlocks.Where(a => a.Url == url).First());
                    await UserConfiguration.RemoveWebsiteBlock(url);
                }
            }
        }

        public string CheckAndFormatUrl(string url)
        {
            // Split to validate domain format (e.g., "example.com")
            var parts = url.Split('.');
            if (parts.Length < 2 || parts.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Invalid URL format. A domain and top-level domain are required.", nameof(url));
            }

            return url;
        }

        public async void ApplyLabelBlock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var label = button.Tag.ToString();
                var websites = StaticDataService.GetTopWebsites(label!);
                var urlBlock = new BlockSettingsUrl();
                var tasks = new List<Task>();
                foreach (var website in websites)
                {
                    var websiteFormatted = CheckAndFormatUrl(website);

                    // Check if the URL already exists in urlBlocks before adding
                    if (!urlBlocks.Any(b => b.Url.Equals(websiteFormatted, StringComparison.OrdinalIgnoreCase)))
                    {
                        tasks.Add(CreateUrlBlock(websiteFormatted, true));
                        urlBlock.UrlBlock.Add(websiteFormatted, new BlockData(BlockType.Full));
                    }
                }
                await Task.WhenAll(tasks);
                await UserConfiguration.StoreBlockedWebsites(urlBlock);
            }
        }
        public async Task CreateUrlBlock(string url, bool bulk = false)
        {
            UrlBlock urlBlock = new UrlBlock(null, url, BlockType.Full);
            urlBlocks.Add(urlBlock);
            if (!bulk)
            {
                await UserConfiguration.StoreBlockedWebsites(new BlockSettingsUrl(url, BlockType.Full));
            }      
        }

        public async void AddUrlBlock_Click(object sender, RoutedEventArgs e)
        {          
            string url = WebsiteTextBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(url))
            {
                ShowInfoMessage("URL can not be empty", InfoEnum.warning);
                WebsiteTextBox.Text = "";
                return;
            }
            if (DoesUrlAlreadyExist(url))
            {
                WebsiteTextBox.Text = "";
                ShowInfoMessage("This URL is already blocked.", InfoEnum.warning);
                return;
            }
            if (!DoesUrlSatisfyRequirements(url))
            {
                WebsiteTextBox.Text = "";
                ShowInfoMessage("URL must have a valid top level domain (.com, .net etc.)", InfoEnum.warning);
                return;
            }
            url = CheckAndFormatUrl(url);
            WebsiteTextBox.Text = "";
            await CreateUrlBlock(url);
        }

        private bool DoesUrlSatisfyRequirements(string url)
        {
            if (!url.Contains("."))
                return false;

            string[] urlParts = url.Split('.');
            string tld = urlParts.Last();

            // Check if the TLD matches any of the recognized domains
            return StaticDataService.RecognizedDomains.Contains(tld);
        }
        private bool DoesUrlAlreadyExist(string url)
        {
            foreach (var item in urlBlocks)
            {
                if (item.Url.Contains(url))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region User action feedback
        private async void ShowInfoMessage(string message, InfoEnum type)
        {
            ErrorInfoBar.Message = message;
            if (type == InfoEnum.warning)
            {
                ErrorInfoBar.Severity = InfoBarSeverity.Error;
                ErrorInfoBar.Title = "Warning";
            }
            else if (type == InfoEnum.info)
            {
                ErrorInfoBar.Severity = InfoBarSeverity.Informational;
                ErrorInfoBar.Title = "Information";
            }
            else
            {
                ErrorInfoBar.Severity = InfoBarSeverity.Success;
                ErrorInfoBar.Title = "Success";
            }
            ErrorInfoBar.IsOpen = true;
            await Task.Delay(1500);
            ErrorInfoBar.IsOpen = false;
        }

        private async Task ShowBlockMessage()
        {
            var dialog = new ContentDialog
            {
                Title = "Action Blocked",
                Content = "Block cant be edited while active.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        private async Task<bool> ShowConfirmationDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Warning",
                Content = message,
                PrimaryButtonText = "Proceed",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        #endregion
    }
}