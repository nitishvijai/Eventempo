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

namespace Eventempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddEvent : Page
    {
        // declare presets
        Event NewYears = new Event("New Year's Day", new DateTime(2020, 1, 1, 0, 0, 0), "Introducing the new decade.", new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)));
        Event Christmas = new Event("Christmas Day", new DateTime(2020, 12, 25, 0, 0, 0), "Ho Ho Ho!", new SolidColorBrush(Color.FromArgb(255, 96, 252, 0)));
        Event Summer = new Event("First Day of Summer", new DateTime(2020, 6, 21, 0, 0, 0), "Suns out, guns out.", new SolidColorBrush(Color.FromArgb(255, 255, 241, 25)));

        // miscellaneous variables
        string[] names;
        bool changesMade = false;

        public AddEvent()
        {
            this.InitializeComponent();
            PresetBox.SelectedItem = NewYearsDay;
            LoadEvent(NewYears);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var eventParam = (string[])e.Parameter;
            names = eventParam;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Event nullEvent = new Event();
            // return to the main screen
            if (changesMade)
            {
                // display warning message
                ContentDialogResult result = await ConfirmCancel.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // discard changes and return to home screen
                    this.Frame.Navigate(typeof(MainPage), null);
                }
                else { /* close dialog box */ }
            }
            else
            {
                // discard changes and return to home screen
                this.Frame.Navigate(typeof(MainPage), null);
            }
        }

        public void LoadEvent(Event e)
        {
            // load events into the comboboxes
            EventNameBox.Text = e.EventName;

            // convert legacy DateTime object into DateTimeOffset
            DateTimeOffset localTime = DateTime.SpecifyKind(e.EventTime, DateTimeKind.Local);

            EventDatePicker.Date = e.EventTime;
            EventTimePicker.Time = (e.EventTime).TimeOfDay;
            NEventColorPicker.Color = (e.EventColor).Color;
            EventDescBox.Text = e.Description;
        }

        public void ResetAllFields()
        {
            // clear all fields if necessary
            EventNameBox.Text = "";
            EventDatePicker.Date = DateTime.Today;
            EventDescBox.Text = "";
        }

        public void ValidateFields()
        {
            // if all fields have been entered, enable the OK button
            if (EventNameBox.Text != "" && EventDatePicker.Date != null && EventDescBox.Text != "" && EventTimePicker.Time != null && EventTimePicker.SelectedTime != null && NEventColorPicker.Color != null)
            {
                OKButton.IsEnabled = true;
            }
            else
            {
                OKButton.IsEnabled = false;
            }
        }

        private void PresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // update comboboxes by selecting preset value
            var comboBoxItem = e.AddedItems[0] as ComboBoxItem;
            var content = comboBoxItem.Content as string;
            switch (content)
            {
                case "New Year's Day":
                    LoadEvent(NewYears);
                    OKButton.IsEnabled = true;
                    break;
                case "Christmas Day":
                    LoadEvent(Christmas);
                    OKButton.IsEnabled = true;
                    break;
                case "First Day of Summer":
                    LoadEvent(Summer);
                    OKButton.IsEnabled = true;
                    break;
                case "No preset":
                    ResetAllFields();
                    NEventColorPicker.Color = Color.FromArgb(255, 0, 0, 0);
                    OKButton.IsEnabled = false;
                    break;
            }
        }

        private void EventDescBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            changesMade = true;
            ValidateFields();
        }

        private void EventNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            changesMade = true;
            ValidateFields();
        }

        private void EventTimeCal_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            changesMade = true;
            ValidateFields();
        }

        private void EventColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            changesMade = true;
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
            if (names.Contains(EventNameBox.Text))
            {
                int numOccur = 0;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].StartsWith(EventNameBox.Text))
                    {
                        numOccur++;
                    }
                }
                EventNameBox.Text = EventNameBox.Text + " (" + numOccur + ")";
            }
            await WriteToFile();
            DateTime final = CombineDateAndTime((EventDatePicker.Date.Value).DateTime, new DateTime().Add(EventTimePicker.Time));
            SolidColorBrush brush = new SolidColorBrush(NEventColorPicker.Color);
            Event createdEvent = new Event(EventNameBox.Text, final, EventDescBox.Text, brush);
            this.Frame.Navigate(typeof(MainPage), createdEvent);
        }

        public async Task WriteToFile()
        {
            DateTime final = CombineDateAndTime((EventDatePicker.Date.Value).DateTime, new DateTime().Add(EventTimePicker.Time));
            string text = EventNameBox.Text + "-!#1#!-" + EventDescBox.Text + "-!#2#!-" + final.ToString("MM/dd/yyyy HH:mm") + "-!#3#!-" + NEventColorPicker.Color.ToString();
            StorageFolder storage = ApplicationData.Current.LocalFolder;
            StorageFile file = await storage.CreateFileAsync("events.txt", CreationCollisionOption.OpenIfExists);

            await FileIO.AppendTextAsync(file, "\n" + text);

        }

        private void EventTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            changesMade = true;
            ValidateFields();
        }

        private void EventDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            changesMade = true;
            ValidateFields();
        }
    }
}
