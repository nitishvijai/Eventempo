using Eventempo.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
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
    public sealed partial class EditEvent : Page
    {
        bool changesMade = false;
        Event mainEvent = new Event();

        string _name, _description;
        TimeSpan _time;
        DateTimeOffset _date;
        Color _color;

        public EditEvent()
        {
            this.InitializeComponent();
            ValidateFields();
            Window.Current.SizeChanged += CurrentWindow_SizeChanged;
        }

        public EditEvent(string name, string description, DateTime time, Color color)
        {
            EventNameBox.Text = name;
            EventDescBox.Text = description;
            EventTimePicker.Time = time.TimeOfDay;
            EventDatePicker.Date = time.Date;
            NEventColorPicker.Color = color;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var eventParam = (Event)e.Parameter;

            EventNameBox.Text = eventParam.EventName;
            _name = eventParam.EventName;
            EventDescBox.Text = eventParam.Description;
            _description = eventParam.Description;
            EventTimePicker.Time = (eventParam.EventTime).TimeOfDay;
            _time = (eventParam.EventTime).TimeOfDay;
            // EventDatePicker.Date = eventParam.EventTime.Date;
            EventDatePicker.Date = new DateTimeOffset(eventParam.EventTime.Date, new TimeSpan(-5, 0, 0));
            _date = EventDatePicker.Date.Value;
            NEventColorPicker.Color = eventParam.EventColor.Color;
            _color = eventParam.EventColor.Color;
            mainEvent = eventParam;
            ValidateFields();
        }

        private void CurrentWindow_SizeChanged(object sender,
                                               Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (e.Size.Width > 640)
            {
                OKButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                OKButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }



            if (e.Size.Width > 1000)
            {
                ColorLabel.Visibility = Visibility.Visible;
                NEventColorPicker.Visibility = Visibility.Visible;
            }
            else
            {
                ColorLabel.Visibility = Visibility.Collapsed;
                NEventColorPicker.Visibility = Visibility.Collapsed;
            }

            if (e.Size.Height > 750)
            {
                OKButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                ColorLabel.Visibility = Visibility.Visible;
                NEventColorPicker.Visibility = Visibility.Visible;
            }
            else
            {
                OKButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
                ColorLabel.Visibility = Visibility.Collapsed;
                NEventColorPicker.Visibility = Visibility.Collapsed;
            }
        }

        public void ValidateFields()
        {
            // if all fields have been entered, enable the OK button


            if (EventNameBox.Text != _name || EventDescBox.Text != _description || EventTimePicker.Time != _time || EventDatePicker.Date != _date || NEventColorPicker.Color != _color)
            {
                changesMade = true;
            }
            else
            {
                changesMade = false;
            }

            if (EventNameBox.Text != "" && EventDatePicker.Date != null && EventTimePicker.SelectedTime != null && NEventColorPicker.Color != null)
            {
                NameWarning.Visibility = Visibility.Collapsed;
                DateWarning.Visibility = Visibility.Collapsed;
                TimeWarning.Visibility = Visibility.Collapsed;
                OKButton.IsEnabled = true;
            }
            else
            {
                OKButton.IsEnabled = false;

                if (EventNameBox.Text == "") { NameWarning.Visibility = Visibility.Visible; }
                else { NameWarning.Visibility = Visibility.Collapsed; }

                if (EventDatePicker.Date == null) { DateWarning.Visibility = Visibility.Visible; }
                else { DateWarning.Visibility = Visibility.Collapsed; }

                if (EventTimePicker.Time == null) { TimeWarning.Visibility = Visibility.Visible; }
                else { TimeWarning.Visibility = Visibility.Collapsed; }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (changesMade)
            {
                // display warning message
                ContentDialogResult result = await ConfirmCancel.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // discard changes and return to home screen
                    this.Frame.Navigate(typeof(MainPage));
                }
                else { /* close dialog box */ }
            }
            else
            {
                // discard changes and return to home screen
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private void EventNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void EventDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            ValidateFields();
        }

        private void EventTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            ValidateFields();
        }

        private void EventDescBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void EventColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            ValidateFields();
        }

        private static DateTime CombineDateAndTime(DateTime dateObj, DateTime timeObj)
        {
            DateTime newDateTime;

            //get timespan from the date object
            TimeSpan spanInDate = dateObj.TimeOfDay;

            //subtract it to set date objects time to 0:00
            dateObj = dateObj.Subtract(spanInDate);

            //now add your newTime to date object
            newDateTime = dateObj.Add(timeObj.TimeOfDay);

            //return new value
            return newDateTime;
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            await WriteToFile();
            // DateTime final = CombineDateAndTime((EventDatePicker.Date.Value).DateTime, new DateTime().Add(EventTimePicker.Time));
            // SolidColorBrush brush = new SolidColorBrush(EventColorPicker.Color);
            // Event createdEvent = new Event(EventNameBox.Text, final, EventDescBox.Text, brush);
            this.Frame.Navigate(typeof(MainPage));
        }

        private void ColorHexagonPicker_ColorChanged(object sender, Color color)
        {
            ValidateFields();
        }

        public async Task WriteToFile()
        {
            DateTime final = CombineDateAndTime((EventDatePicker.Date.Value).DateTime, new DateTime().Add(EventTimePicker.Time));
            string fileText = "";
            string text = EventNameBox.Text + "-!#1#!-" + EventDescBox.Text + "-!#2#!-" + final.ToString("MM/dd/yyyy HH:mm") + "-!#3#!-" + NEventColorPicker.Color.ToString();
            StorageFolder storage = ApplicationData.Current.LocalFolder;
            StorageFile file = await storage.CreateFileAsync("events.txt", CreationCollisionOption.OpenIfExists);

            IList<string> lines = await FileIO.ReadLinesAsync(file);

            await FileIO.WriteTextAsync(file, "");
            int textCount = lines.Count;
            for (int i = 0; i < textCount; i++)
            {
                if (lines[i].StartsWith(_name + "-!#1#!-") && lines[i] != null)
                {
                    continue;
                }
                fileText = lines[i];
                await FileIO.AppendTextAsync(file, "\n" + fileText);
            }

            await FileIO.AppendTextAsync(file, "\n" + text);
        }


    }
}
