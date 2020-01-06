using Eventempo.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Eventempo.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EventView : Page
    {
        DispatcherTimer timer = new DispatcherTimer();
        Event currEvent;
        public EventView()
        {
            this.InitializeComponent();

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(520, 507));
        }

        public void DispatcherTimerSetup()
        {
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var eventParam = (Event)e.Parameter;

            NameLabel.Foreground = DayLabel.Foreground;
            DateLabel.Foreground = DayLabel.Foreground;

            NameLabel.Text = eventParam.EventName;
            DateLabel.Text = eventParam.EventTime.ToString("dddd, MMMM dd yyyy h:mm tt");
            YearLabel.Text = eventParam.years.ToString();
            MonthLabel.Text = eventParam.months.ToString();
            DayLabel.Text = eventParam.days.ToString();
            HourLabel.Text = eventParam.hours.ToString();
            MinuteLabel.Text = eventParam.minutes.ToString();
            SecondLabel.Text = eventParam.seconds.ToString();
            this.Background = eventParam.EventColor;

            if (((eventParam.EventColor.Color.R * 0.299 + eventParam.EventColor.Color.G * 0.587 + eventParam.EventColor.Color.B * 0.114) / 255) > 0.5)
            {
                NameLabel.Foreground = new SolidColorBrush(Colors.Black);
                DateLabel.Foreground = new SolidColorBrush(Colors.Black);
                YearLabel.Foreground = new SolidColorBrush(Colors.Black);
                MonthLabel.Foreground = new SolidColorBrush(Colors.Black);
                DayLabel.Foreground = new SolidColorBrush(Colors.Black);
                HourLabel.Foreground = new SolidColorBrush(Colors.Black);
                MinuteLabel.Foreground = new SolidColorBrush(Colors.Black);
                SecondLabel.Foreground = new SolidColorBrush(Colors.Black);

                YearText.Foreground = new SolidColorBrush(Colors.Black);
                MonthText.Foreground = new SolidColorBrush(Colors.Black);
                DayText.Foreground = new SolidColorBrush(Colors.Black);
                HourText.Foreground = new SolidColorBrush(Colors.Black);
                MinuteText.Foreground = new SolidColorBrush(Colors.Black);
                SecondText.Foreground = new SolidColorBrush(Colors.Black);

                BackButton.Foreground = new SolidColorBrush(Colors.Black);
            }
            BackButton.IsEnabled = this.Frame.CanGoBack;
            currEvent = eventParam;

            DispatcherTimerSetup();
        }

        private void Timer_Tick(object sender, object e)
        {
            currEvent.CalculateDateDifference();
            YearLabel.Text = currEvent.years.ToString();
            MonthLabel.Text = currEvent.months.ToString();
            DayLabel.Text = currEvent.days.ToString();
            HourLabel.Text = currEvent.hours.ToString();
            MinuteLabel.Text = currEvent.minutes.ToString();
            SecondLabel.Text = currEvent.seconds.ToString();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        private bool On_BackRequested()
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                return true;
            }
            return false;
        }
    }
}
