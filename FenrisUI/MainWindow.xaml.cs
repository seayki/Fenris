using Fenris;
using Fenris.DiscoveryServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using static Fenris.BlockSettings;
using Process = Fenris.Process;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FenrisUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {

        public List<Process> FilteredProcesses { get; set; } = new();
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();
        private HashSet<string> selectedDays = new HashSet<string>();
        private Dictionary<string, List<TimePicker>> dayTimePickers = new Dictionary<string, List<TimePicker>>();

        private readonly IDiscoveryService _discoveryService;
        private readonly IProcessTerminator _processTerminator;
        private readonly IUserConfiguration _userConfiguration;
        private BlockSettings BlockSettings { get; set; } 
        private List<string> BlockedWebsites { get; set; } = new List<string>();    

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
                    OnPropertyChanged(nameof(IsBlocked)); // Notify UI of change
                }
            }
        }

        // BlockStatusColor property for the Ellipse
        private SolidColorBrush _blockStatusColor = new SolidColorBrush(Colors.Red); // Default to red
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
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public MainWindow(IDiscoveryService discoveryService, IProcessTerminator processTerminator, IUserConfiguration userConfiguration)
        {
            
            _discoveryService = discoveryService;
            _processTerminator = processTerminator;
            _userConfiguration = userConfiguration;

            // Initialize time pickers dictionary
            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            {
                dayTimePickers[day] = new List<TimePicker>();
            }

            // Load settings
            if (this._userConfiguration.LoadBlockSettings() != null)
            {
                BlockSettings = this._userConfiguration.LoadBlockSettings();
                IsBlocked = BlockSettings.IsBlockActive();
                UpdateBlockStatusColor();
            }
            if (this._userConfiguration.LoadBlockedWebsites() != null)
            {
                BlockedWebsites = this._userConfiguration.LoadBlockedWebsites();
            }   

            this.InitializeComponent();
            TagList.ItemsSource = Tags;
            //StartBlockingTask();
        }

        private void UpdateBlockStatusColor()
        {
            BlockStatusColor = IsBlocked ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }

        private void MainScrollViewer_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - delta);
                e.Handled = true; // Prevent nested controls from stealing the event
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTag();
        }

        private void TagInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddTag();
                e.Handled = true;
            }
        }

        private void AddTag()
        {
            string newTag = TagInput.Text.Trim();
            if (!string.IsNullOrEmpty(newTag) && !Tags.Contains(newTag))
            {
                // Update processes list and add filtered processes to the filtered list
                var processesToFilter = Processes.Where(Processes => Processes.Name.Contains(newTag)).ToList();
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
                // Remove from filtered processes and add back to the main list 
                var processesToUnfilter = FilteredProcesses.Where(Processes => Processes.Name.Contains(tag)).ToList();
                processesToUnfilter.ForEach(processesToUnfilter => FilteredProcesses.Remove(processesToUnfilter));
                processesToUnfilter.ForEach(p => Processes.Add(p));

                Tags.Remove(tag);
            }
        }


        private async Task<List<Process>> DiscoverService()
        {
            List<Process> processes = await Task.Run(() => _discoveryService.DiscoverGames());
            return processes;
        }

        private async void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Pivot pivot && pivot.SelectedItem is PivotItem selectedItem)
            {
                if (selectedItem.Header.ToString() == "Apps")
                {
                    var discoveredProcesses = await DiscoverService();
                    Processes.Clear();
                    foreach (var process in discoveredProcesses)
                    {
                        Processes.Add(process);
                    }
                }
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
                            // Deselect the item
                            SelectedProcesses.Remove(process);
                            stackPanel.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // White
                        }
                        else
                        {
                            // Select the item
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

                if (timePickersPanel == null)
                {
                    return;
                }

                if (selectedDays.Contains(day))
                {
                    selectedDays.Remove(day);
                    button.Background = new SolidColorBrush(Color.FromArgb(255, 224, 224, 224));
                    button.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                    timePickersPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    selectedDays.Add(day);
                    button.Background = new SolidColorBrush(Color.FromArgb(255, 39, 40, 51));
                    button.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    timePickersPanel.Visibility = Visibility.Visible;

                    timePickersPanel.Children.Clear();
                    dayTimePickers[day].Clear();

                    AddTimeBox(day, timePickersPanel);

                    var addTimeButton = new Button
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                        Background = new SolidColorBrush(Color.FromArgb(255, 11, 167, 105)),
                        Content = "Add Time",
                        Width = 140,
                        Tag = day,
                        HorizontalAlignment = HorizontalAlignment.Center,
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

        private void AddTimeBox(string day, StackPanel panel)
        {
            // Create the container for the time box
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 236, 238, 236)),
                BorderBrush =  new SolidColorBrush(Color.FromArgb(255, 39, 40, 51)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Width = 270 // Added width to control the box size
            };

            // Create StackPanel for vertical layout
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 10
            };

            // From TimePicker
            var fromTimePicker = new TimePicker
            {
                Header = "From",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                ClockIdentifier = "24HourClock",
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            };

            // To TimePicker
            var toTimePicker = new TimePicker
            {

                Header = "To",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                ClockIdentifier = "24HourClock",
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            };

            // Delete Button
            var deleteButton = new Button
            {
                Content = "Delete",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Dubai Light"),
                Background = new SolidColorBrush(Color.FromArgb(255, 250, 109, 122)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(1.4), // Slightly thicker border for emphasis
                HorizontalAlignment = HorizontalAlignment.Right // Align to right for better appearance
            };
            // Store both TimePickers in the day's list
            dayTimePickers[day].Add(fromTimePicker);
            dayTimePickers[day].Add(toTimePicker);

            // Handle delete button click
            deleteButton.Click += (s, e) =>
            {
                panel.Children.Remove(border);
                dayTimePickers[day].Remove(fromTimePicker);
                dayTimePickers[day].Remove(toTimePicker);
            };

            // Add controls to stack panel
            stackPanel.Children.Add(fromTimePicker);
            stackPanel.Children.Add(toTimePicker);
            stackPanel.Children.Add(deleteButton);

            // Add stack panel to border
            border.Child = stackPanel;

            // Insert before the Add Time button (last element)
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
            _userConfiguration.StoreBlockSettings(blockSettings);
        }

        private async Task UpdateBlockStatus()
        {
            while (true)
            {
                BlockSettings = _userConfiguration.LoadBlockSettings();
                IsBlocked = BlockSettings.IsBlockActive();
                await Task.Delay(1000);
            }       
        }
    }
}
 