/*
    Event.cs
    Eventempo

    Created by Nitish Vijai on 12/20/2019
    Copyright (c) 2019 Nitish Vijai. All rights reserved.
 */
using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Eventempo.Model
{
    public class Event : INotifyPropertyChanged
    {
        // Misc variables
        private int _seconds, _minutes, _hours, _days, _months, _years;

        // variables to implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Event fields for time
        public int years
        {
            get { return _years; }
            set
            {
                _years = value;
                OnPropertyChanged("years");
            }
        }
        public int months
        {
            get { return _months; }
            set
            {
                _months = value;
                OnPropertyChanged("months");
            }
        }
        public int days
        {
            get { return _days; }
            set
            {
                _days = value;
                OnPropertyChanged("days");
            }
        }
        public int hours
        {
            get { return _hours; }
            set
            {
                _hours = value;
                OnPropertyChanged("hours");
            }
        }
        public int minutes
        {
            get { return _minutes; }
            set
            {
                _minutes = value;
                OnPropertyChanged("minutes");
            }
        }
        public int seconds
        {
            get { return _seconds; }
            set
            {
                _seconds = value;
                OnPropertyChanged("seconds");
            }
        }

        // event field represents the name of the event
        public string EventName { get; set; }

        // event field represents the date/time of the event
        public DateTime EventTime { get; set; }

        // event field represents the description for the event
        public string Description { get; set; }

        // event field represents the color associated with the event
        public SolidColorBrush EventColor { get; set; }

        // event field represents if timer has been reached
        public bool TimerReached { get; set; }

        // default constructor
        public Event()
        {
            EventName = "New Year's Day Example Event (Year 2000)";
            EventTime = new DateTime(2000, 1, 1, 0, 0, 0);
            Description = "Entering the new millennium with a bang.";
            EventColor = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            CalculateDateDifference();
        }

        // main constructor
        public Event(string eventName, DateTime eventTime, string description, SolidColorBrush eventColor)
        {
            EventName = eventName;
            EventTime = eventTime;
            EventTime = DateTime.SpecifyKind(EventTime, DateTimeKind.Unspecified);
            Description = description;
            EventColor = eventColor;
            CalculateDateDifference();
        }

        // calculates the initial difference between today and the target end time
        public void CalculateDateDifference()
        {
            DateTime zeroTime = new DateTime(1, 1, 1);
            DateTime today = DateTime.Now;
            DateTime target = this.EventTime;

            if (target < today)
            {
                TimerReached = true;
            }

            var diff = DateTimeSpan.CompareDates(today, target);

            if (TimerReached == false)
            {
                days = diff.Days;
                months = diff.Months;
                years = diff.Years;

                hours = diff.Hours;
                minutes = diff.Minutes;
                seconds = diff.Seconds;
            }
        }

        // Holds time values at zero whenever necessary
        public void HoldatZero()
        {
            years = 0;
            months = 0;
            days = 0;
            hours = 0;
            minutes = 0;
            seconds = 0;
        }
    }

    public struct DateTimeSpan
    {
        public int Years { get; }
        public int Months { get; }
        public int Days { get; }
        public int Hours { get; }
        public int Minutes { get; }
        public int Seconds { get; }
        public int Milliseconds { get; }

        public DateTimeSpan(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
        {
            Years = years;
            Months = months;
            Days = days;
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
            Milliseconds = milliseconds;
        }

        enum Phase { Years, Months, Days, Done }

        public static DateTimeSpan CompareDates(DateTime date1, DateTime date2)
        {
            if (date2 < date1)
            {
                var sub = date1;
                date1 = date2;
                date2 = sub;
            }

            DateTime current = date1;
            int years = 0;
            int months = 0;
            int days = 0;

            Phase phase = Phase.Years;
            DateTimeSpan span = new DateTimeSpan();
            int officialDay = current.Day;

            while (phase != Phase.Done)
            {
                switch (phase)
                {
                    case Phase.Years:
                        if (current.AddYears(years + 1) > date2)
                        {
                            phase = Phase.Months;
                            current = current.AddYears(years);
                        }
                        else
                        {
                            years++;
                        }
                        break;
                    case Phase.Months:
                        if (current.AddMonths(months + 1) > date2)
                        {
                            phase = Phase.Days;
                            current = current.AddMonths(months);
                            if (current.Day < officialDay && officialDay <= DateTime.DaysInMonth(current.Year, current.Month))
                                current = current.AddDays(officialDay - current.Day);
                        }
                        else
                        {
                            months++;
                        }
                        break;
                    case Phase.Days:
                        if (current.AddDays(days + 1) > date2)
                        {
                            current = current.AddDays(days);
                            var timespan = date2 - current;
                            span = new DateTimeSpan(years, months, days, timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
                            phase = Phase.Done;
                        }
                        else
                        {
                            days++;
                        }
                        break;
                }
            }

            return span;
        }
    }
}
