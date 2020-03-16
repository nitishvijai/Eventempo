using System;
using Eventempo.Model;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.IO;
using Windows.UI.Xaml.Data;
using System.Linq;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Eventempo.View;
using System.Threading;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Core;
using Windows.Services.Store;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Eventempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // miscellaneous variables
        ObservableCollection<Event> dataList = new ObservableCollection<Event>();
        DispatcherTimer timer = new DispatcherTimer();
        int eventCount = 0;
        private StoreContext context = null;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        // page initialization
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.SizeChanged += CurrentWindow_SizeChanged;
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
            dataList.Clear();
            ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = (Color)Resources["SystemAccentColor"];
            ApplicationView.GetForCurrentView().TitleBar.ButtonBackgroundColor = (Color)Resources["SystemAccentColor"];
        
            
            if (localSettings.Values.ContainsKey("sortMethod"))
            {
                var savedItem = localSettings.Values["sortMethod"];
                foreach (var item in SortBox.Items)
                {
                    if ((item as ComboBoxItem).Content.Equals(savedItem))
                    {
                        SortBox.SelectedItem = item;
                    }
                }
            }
        }

        public async Task CheckForUpdatesAsync()
        {
            if (context == null)
            {
                context = StoreContext.GetDefault();
            }

            IReadOnlyList<StorePackageUpdate> updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();

            if (updates.Count > 0)
            {
                MessageDialog dialog = new MessageDialog("There is a new update available.", "Download and Install?");
                dialog.Commands.Add(new UICommand("Open Store"));
                dialog.Commands.Add(new UICommand("Cancel"));
                IUICommand command = await dialog.ShowAsync();
                if (command.Label.Equals("Open Store", StringComparison.CurrentCultureIgnoreCase))
                {
                    bool result = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?productid=9PCFMSX2G386"));
                }
            }
            else
            {
                MessageDialog dialog = new MessageDialog("No new updates found.", "You're Good");
                dialog.Commands.Add(new UICommand("Close"));
                IUICommand command = await dialog.ShowAsync();
            }
        }


        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.ToString().Contains("Down"))
            {
                var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.N:
                            AddButton_Click(this, null);
                            break;
                    }
                }
            }
        }

        // occurs after adding/editing an event
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter.GetType() != typeof(string))
            {
                dataList.Add((Event)e.Parameter);
                EventList.ItemsSource = dataList;
                await ReadTextFile();
            }
        }

        public async Task<string[]> OpenTextFile()
        {
            var lines = new List<string>();
            StorageFolder storage = ApplicationData.Current.LocalFolder;
            StorageFile file;
            if (await IsFilePresent())
            {
                file = await storage.GetFileAsync("events.txt");
            }
            else
            {
                file = await storage.CreateFileAsync("events.txt");
            }

            using (var reader = File.OpenText(file.Path))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines.ToArray();
        }

        public async Task<bool> IsFilePresent()
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync("events.txt");
            return item != null;
        }

        // reads events.txt for saved events, will readd to ListView after opening app or adding event
        public async Task ReadTextFile()
        {
            Binding list = new Binding();
            list.Source = dataList;
            list.Mode = BindingMode.OneWay;

            var lines = await OpenTextFile();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "") continue;

                int found = lines[i].IndexOf("-!#1#!-");
                string eventName = lines[i].Substring(0, found);

                int found2 = lines[i].IndexOf("-!#2#!-");
                string eventDesc = lines[i].Substring(found + 7, found2 - found - 7);

                int found3 = lines[i].IndexOf("-!#3#!-");
                string rawTime = lines[i].Substring(found2 + 7, found3 - found2 - 7);
                DateTime date = DateTime.Parse(rawTime);

                string hex = lines[i].Substring(found3 + 7, lines[i].Length - found3 - 7);
                SolidColorBrush eventColor = GetSolidColorBrush(hex);

                Event addEvent = new Event(eventName, date, eventDesc, eventColor);
                if (!dataList.Any(t => t.EventName == eventName))
                {
                    dataList.Add(addEvent);
                    EventList.SetBinding(ListView.ItemsSourceProperty, list);
                }
            }
            EventList.SetBinding(ListView.ItemsSourceProperty, list);
            DispatcherTimerSetup();
            string text = " timers running";
            eventCount = 0;
            foreach (Event i in EventList.Items)
            {
                if (!i.TimerReached)
                {
                    eventCount++;
                }
            }
            if (eventCount == 1)
            {
                text = " timer running";
            }
            RunningTimers.Text = eventCount + text;
            if (EventList.Items.Count > 0)
            {
                DeleteAllButton.Visibility = Visibility.Visible;
            }
            else
            {
                DeleteAllButton.Visibility = Visibility.Collapsed;
            }
        }

        // converts hex color value into SolidColorBrush for each Event color
        public SolidColorBrush GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            return myBrush;
        }

        // starts/stops timer when reading in events
        public void DispatcherTimerSetup()
        {
            timer.Stop();
            timer.Tick -= Timer_Tick;
            foreach (var item in dataList)
            {
                item.CalculateDateDifference();
            }
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        public void ShowTimerReachedToast(string timerName, DateTime timerDate)
        {
            var toastContent = new ToastContent()
            {
                Scenario = ToastScenario.Alarm,
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = timerName
                            },
                            new AdaptiveText()
                            {
                                Text = "It's time for your event!"
                            },
                            new AdaptiveText()
                            {
                                Text = timerDate.ToString("h:mm tt")
                            }
                        }
                    }
                },
                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        new ToastButtonDismiss("Dismiss")
                    }
                },
                Audio = new ToastAudio()
                {
                    Src = new Uri("ms-winsoundevent:Notification.Looping.Alarm"),
                    Loop = true
                }
            };

            // Create the toast notification
            var toastNotif = new ToastNotification(toastContent.GetXml());

            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
        }
        public void ShowFiveMinuteToast(string timerName, DateTime timerDate)
        {
            var toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = timerName
                            },
                            new AdaptiveText()
                            {
                                Text = "Your timer will sound in 5 minutes."
                            },
                            new AdaptiveText()
                            {
                                Text = "Ring at: " + timerDate.ToString("h:mm tt")
                            }
                        }
                    }
                },
                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        new ToastButtonDismiss("Dismiss")
                    }
                }
            };

            // Create the toast notification
            var toastNotif = new ToastNotification(toastContent.GetXml());

            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
        }

        public void SortListView()
        {
            if (SortBox.SelectedItem == NameItem)
            {
                EventList.ItemsSource = (from Event in dataList
                                        orderby Event.EventName
                                        select Event); 
            }
            else if (SortBox.SelectedItem == DateItem)
            {
                EventList.ItemsSource = (from Event in dataList
                                         orderby Event.EventTime
                                         select Event);
            }   
            else if (SortBox.SelectedItem == ColorItem)
            {
                EventList.ItemsSource = (from Event in dataList
                                         orderby (Event.EventColor.Color.R + Event.EventColor.Color.G + Event.EventColor.Color.B)
                                         select Event);
            }
            else if (SortBox.SelectedItem == CompleteItem)
            {
                EventList.ItemsSource = (from Event in dataList
                                         orderby Event.TimerReached
                                         select Event);
            }
        }

        // countdown timer
        public void Timer_Tick(object sender, object e)
        {
            string text = " timers running";
            foreach (var item in dataList)
            {
                if (!item.TimerReached)
                {
                    item.CalculateDateDifference();
                }
                // checks if timer is less than 5 minutes or if it reaches 0
                if (eventCount > 0 && item.years == 0 && item.months == 0 && item.days == 0 && item.hours == 0 & item.minutes == 5 & item.seconds == 0)
                {
                    ShowFiveMinuteToast(item.EventName, item.EventTime);
                }
                else if (eventCount > 0 && item.years == 0 && item.months == 0 && item.days == 0 && item.hours == 0 && item.minutes == 0 && item.seconds == 0 && item.TimerReached == false)
                {
                    // invoke end time method
                    item.HoldatZero();
                    eventCount--;
                    ShowTimerReachedToast(item.EventName, item.EventTime);
                    item.TimerReached = true;
                }

                if (eventCount == 1)
                {
                    text = " timer running";
                }
                RunningTimers.Text = eventCount + text;

                if (item.TimerReached == true)
                {
                    item.years = 0;
                    item.months = 0;
                    item.days = 0;
                    item.hours = 0;
                    item.minutes = 0;
                    item.seconds = 0;
                }

                
            }

        }

        private void CurrentWindow_SizeChanged(object sender,
                                               Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            
        }

        private async void DeleteAllButton_Click(object sender, 
                                                 RoutedEventArgs e)
        {
            ContentDialogResult result = await ConfirmDeletionAll.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // delete all events
                StorageFolder storage = ApplicationData.Current.LocalFolder;
                StorageFile file = await storage.CreateFileAsync("events.txt", CreationCollisionOption.OpenIfExists);

                await FileIO.WriteTextAsync(file, "");

                dataList.Clear();
            }
            else { /* close dialog box */ }

            await ReadTextFile();
        }

        private async void DeleteSelectedButton_Click(object sender, 
                                                      RoutedEventArgs e)
        {
            ContentDialogResult result = await ConfirmDeletionOne.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // delete just this event
                StorageFolder storage = ApplicationData.Current.LocalFolder;
                StorageFile file = await storage.CreateFileAsync("events.txt", CreationCollisionOption.OpenIfExists);

                IList<string> lines = await FileIO.ReadLinesAsync(file);

                await FileIO.WriteTextAsync(file, "");

                string text = "";
                int textCount = lines.Count;
                var selEvent = ConvertEvent(EventList.SelectedItem);
                for (int i = 0; i < textCount; i++)
                {
                    if (lines[i].StartsWith(selEvent.EventName + "-!#1#!-") && lines[i] != null)
                    {
                        continue;
                    }
                    if (i == 0) { text = lines[i]; }
                    else { text = "\n" + lines[i]; }

                    await FileIO.AppendTextAsync(file, text);
                }
                dataList.Remove(selEvent);
            }
            else { /* close dialog box */ }

            await ReadTextFile();
        }

        public Event ConvertEvent(object value)
        {
            return value as Event;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string[] eventnames = new string[dataList.Count];
            for (int i = 0; i < dataList.Count; i++)
            {
                eventnames[i] = ConvertEvent(EventList.Items[i]).EventName;
            }
            this.Frame.Navigate(typeof(AddEvent), eventnames);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await ReadTextFile();

            
        }

        private void EventList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventList.SelectedItem != null)
            {
                DeleteSelectedButton.Visibility = Visibility.Visible;
                ViewButton.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
            }
            else
            {
                DeleteSelectedButton.Visibility = Visibility.Collapsed;
                ViewButton.Visibility = Visibility.Collapsed;
                EditButton.Visibility = Visibility.Collapsed;
            }
        }

        private void EventList_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (EventList.SelectedItem != null)
            {
                this.Frame.Navigate(typeof(EditEvent), EventList.SelectedItem);
            }
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventList.SelectedItem != null)
            {
                this.Frame.Navigate(typeof(EventView), EventList.SelectedItem);
            }
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            var openReview = Launcher.LaunchUriAsync(new Uri("mailto:nitishv.apps@outlook.com"));
        }

        private void Page_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control)
            {

            }
        }

        private void ViewButton_PreviewKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Shift);
            var shiftPressed = shiftState == CoreVirtualKeyStates.Down || shiftState == (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
            if (shiftPressed && e.Key == VirtualKey.F)
            {
                ViewButton_Click(sender, null);
                e.Handled = true;
            }
        }

        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboboxItem = e.AddedItems[0] as ComboBoxItem;
            comboboxItem.IsSelected = true;
            var content = comboboxItem.Content as string;
            localSettings.Values["sortMethod"] = content; 
            
            SortListView();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventList.SelectedItem != null)
            {
                this.Frame.Navigate(typeof(EditEvent), EventList.SelectedItem);
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Update.Label = "Checking For Updates";
            await CheckForUpdatesAsync();
            Update.Label = "Check for updates";
        }
    }
}
