using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Beamable.Common.Content;
using Beamable.Editor.Schedules;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests.Content
{
    public class SchedulesTests
    {
        [Test]
        public void Event_Schedule_Daily_Mode_Test()
        {
            String warningHeader = "Daily event schedule:";
            bool received = false;

            void ScheduleReceived(Schedule schedule)
            {
                received = true;
                bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);
                Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
                Assert.IsTrue(schedule.definitions.Count == 1, $"{warningHeader} should have only one definition");
            }

            EventScheduleWindow window = new EventScheduleWindow();
            window.Refresh();
            window.ModeComponent.Set(0);
            window.StartTimeComponent.Set(DateTime.Now);
            window.NeverExpiresComponent.Value = true;
            window.ActiveToDateComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.ActiveToHourComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.OnScheduleUpdated += ScheduleReceived;
            window.InvokeTestConfirm();
            window.OnScheduleUpdated -= ScheduleReceived;
            Assert.IsTrue(received, "Schedule not received. Test failed");
        }

        [Test]
        public void Event_Schedule_Days_Mode_Test()
        {
            String warningHeader = "Days event schedule:";
            bool received = false;

            void ScheduleReceived(Schedule schedule)
            {
                received = true;
                bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);
                List<string> days = schedule.definitions[0].dayOfWeek;

                Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
                Assert.IsTrue(schedule.definitions.Count == 1, $"{warningHeader} should have only one definition");
                Assert.IsTrue(days.Count > 0, $"{warningHeader} minimum one day should be selected");

                foreach (string day in days)
                {
                    bool isDayParsed = int.TryParse(day, out int parsedDayValue);
                    Assert.IsTrue(isDayParsed, $"{warningHeader} problem with parsing {day} in days list");
                    Assert.IsTrue(parsedDayValue <= 7, $"{warningHeader} parsed day value should be less or equal 7");
                }

                if (schedule.definitions.Count > 0)
                {
                    TestHour(warningHeader, schedule.definitions[0]);
                }
            }

            EventScheduleWindow window = new EventScheduleWindow();
            window.Refresh();
            window.ModeComponent.Set(1);
            window.StartTimeComponent.Set(DateTime.Now);
            window.DaysComponent.SetSelectedDays(new List<string> {"1", "3", "5"});
            window.NeverExpiresComponent.Value = true;
            window.ActiveToDateComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.ActiveToHourComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.OnScheduleUpdated += ScheduleReceived;
            window.InvokeTestConfirm();
            window.OnScheduleUpdated -= ScheduleReceived;
            Assert.IsTrue(received, "Schedule not received. Test failed");
        }

        [Test]
        public void Event_Schedule_Dates_Mode_Test()
        {
            String warningHeader = "Dates event schedule:";
            bool received = false;

            void ScheduleReceived(Schedule schedule)
            {
                received = true;
                bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);

                Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
                Assert.IsTrue(schedule.definitions.Count > 0, $"{warningHeader} should have at least on definition");

                List<string> days = schedule.definitions[0].dayOfMonth;
                Assert.IsTrue(days.Count > 0, $"{warningHeader} minimum one day should be selected");

                foreach (string day in days)
                {
                    bool isDayParsed = int.TryParse(day, out int parsedDayValue);
                    Assert.IsTrue(isDayParsed, $"{warningHeader} problem with parsing {day} in days list");
                    Assert.IsTrue(parsedDayValue <= 7, $"{warningHeader} parsed day value should be less or equal 7");
                }

                if (schedule.definitions.Count > 0)
                {
                    TestHour(warningHeader, schedule.definitions[0]);
                }
            }

            EventScheduleWindow window = new EventScheduleWindow();
            window.Refresh();
            window.ModeComponent.Set(2);
            window.StartTimeComponent.Set(DateTime.Now);
            window.CalendarComponent.Calendar.SetInitialValues(new List<string>
            {
                "05-10-2021",
                "10-11-2021",
                "12-12-2022"
            });
            window.NeverExpiresComponent.Value = false;
            window.ActiveToDateComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.ActiveToHourComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.OnScheduleUpdated += ScheduleReceived;
            window.InvokeTestConfirm();
            window.OnScheduleUpdated -= ScheduleReceived;
            Assert.IsTrue(received, "Schedule not received. Test failed");
        }

        [Test]
        public void Listing_Schedule_Daily_Mode_Test()
        {
            String warningHeader = "Daily listing schedule:";
            bool received = false;

            void ScheduleReceived(Schedule schedule)
            {
                received = true;
                Assert.IsTrue(DateTime.TryParse(schedule.activeFrom, out DateTime _),
                    $"{warningHeader} problem with parsing activeFrom field");
                Assert.IsTrue(schedule.definitions.Count > 0 && schedule.definitions.Count <= 3,
                    $"{warningHeader} definitions amount should be greater than 0 and less or equal to 3");

                foreach (ScheduleDefinition scheduleDefinition in schedule.definitions)
                {
                    string minuteString = scheduleDefinition.minute[0];
                    string hoursString = scheduleDefinition.hour[0];
                    bool minutesMatchPattern = Regex.IsMatch(minuteString, "\\b([0-9]|[1-5][0-9])\\b") ||
                                               Regex.IsMatch(minuteString, "/*");
                    bool hoursMatchPattern =
                        Regex.IsMatch(hoursString, "\\b([0-9]|1[0-9]|2[0-3])-([0-9]|1[0-9]|2[0-3])\\b") || 
                        Regex.IsMatch(hoursString, "\\b([0-9]|1[0-9]|2[0-3])\\b");
                    Assert.IsTrue(minutesMatchPattern, $"{warningHeader} minutes doesn't match pattern");
                    Assert.IsTrue(hoursMatchPattern, $"{warningHeader} hours doesn't match pattern");
                }
            }

            ListingScheduleWindow window = new ListingScheduleWindow();
            window.Refresh();
            window.ModeComponent.Set(0);
            window.AllDayComponent.Value = false;
            window.PeriodFromHourComponent.Set(DateTime.Now);
            window.PeriodToHourComponent.Set(DateTime.Now + TimeSpan.FromHours(2));
            window.NeverExpiresComponent.Value = true;
            window.ActiveToDateComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.ActiveToHourComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.OnScheduleUpdated += ScheduleReceived;
            window.InvokeTestConfirm();
            window.OnScheduleUpdated -= ScheduleReceived;
            Assert.IsTrue(received, "Schedule not received. Test failed");
        }

        [Test]
        public void Listing_Schedule_Days_Mode_Test()
        {
            String warningHeader = "Days listing schedule:";
            bool received = false;

            void ScheduleReceived(Schedule schedule)
            {
                received = true;
                bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);

                Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
                Assert.IsTrue(schedule.definitions.Count > 0 && schedule.definitions.Count <= 3,
                    $"{warningHeader} definitions amount should be greater than 0 and less or equal to 3");

                List<string> days = schedule.definitions[0].dayOfWeek;
                Assert.IsTrue(days.Count > 0 && days.Count < 7,
                    $"{warningHeader} minimum one and maximum 6 days should be selected");

                foreach (string day in days)
                {
                    bool isDayParsed = int.TryParse(day, out int parsedDayValue);
                    Assert.IsTrue(isDayParsed, $"{warningHeader} problem with parsing {day} in days list");
                    Assert.IsTrue(parsedDayValue <= 7, $"{warningHeader} parsed day value should be less or equal 7");
                }

                TestPeriod(schedule, warningHeader);
            }

            ListingScheduleWindow window = new ListingScheduleWindow();
            window.Refresh();
            window.ModeComponent.Set(1);
            window.DaysComponent.SetSelectedDays(new List<string> {"1", "3", "5"});
            window.AllDayComponent.Value = false;
            window.PeriodFromHourComponent.Set(DateTime.Now);
            window.PeriodToHourComponent.Set(DateTime.Now + TimeSpan.FromHours(2));
            window.NeverExpiresComponent.Value = false;
            window.ActiveToDateComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.ActiveToHourComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.OnScheduleUpdated += ScheduleReceived;
            window.InvokeTestConfirm();
            window.OnScheduleUpdated -= ScheduleReceived;
            Assert.IsTrue(received, "Schedule not received. Test failed");
        }

        [Test]
        public void Listing_Schedule_Dates_Mode_Test()
        {
            String warningHeader = "Dates event schedule:";
            bool received = false;

            void ScheduleReceived(Schedule schedule)
            {
                received = true;
                bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);

                Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
                Assert.IsTrue(schedule.definitions.Count > 0,
                    $"{warningHeader} definitions amount should be greater than 0");

                List<string> days = schedule.definitions[0].dayOfMonth;
                Assert.IsTrue(days.Count > 0, $"{warningHeader} minimum one day should be selected");

                foreach (string day in days)
                {
                    bool isDayParsed = int.TryParse(day, out int parsedDayValue);
                    Assert.IsTrue(isDayParsed, $"{warningHeader} problem with parsing {day} in days list");
                    Assert.IsTrue(parsedDayValue <= 7, $"{warningHeader} parsed day value should be less or equal 7");
                }

                TestPeriod(schedule, warningHeader);
            }

            ListingScheduleWindow window = new ListingScheduleWindow();
            window.Refresh();
            window.ModeComponent.Set(2);
            window.CalendarComponent.Calendar.SetInitialValues(new List<string>
            {
                "05-10-2021",
                "10-11-2021",
                "12-12-2022"
            });
            window.AllDayComponent.Value = false;
            window.PeriodFromHourComponent.Set(DateTime.Now);
            window.PeriodToHourComponent.Set(DateTime.Now + TimeSpan.FromHours(2));
            window.NeverExpiresComponent.Value = false;
            window.ActiveToDateComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.ActiveToHourComponent.Set(DateTime.Now + TimeSpan.FromDays(2));
            window.OnScheduleUpdated += ScheduleReceived;
            window.InvokeTestConfirm();
            window.OnScheduleUpdated -= ScheduleReceived;
            Assert.IsTrue(received, "Schedule not received. Test failed");
        }

        private void TestHour(string warningHeader, ScheduleDefinition definition)
        {
            bool isHourParsed = int.TryParse(definition.hour[0], out int parsedHour);
            bool isMinuteParsed = int.TryParse(definition.minute[0], out int parsedMinute);
            bool isSecondParsed = int.TryParse(definition.second[0], out int parsedSecond);

            Assert.IsTrue(isHourParsed, $"{warningHeader} problem with parsing hour");
            Assert.IsTrue(isMinuteParsed, $"{warningHeader} problem with parsing minute");
            Assert.IsTrue(isSecondParsed, $"{warningHeader} problem with parsing second");

            Assert.IsTrue(parsedHour >= 0 && parsedHour < 24,
                $"{warningHeader} hour should be greater or equal 0 and less than 24");
            Assert.IsTrue(parsedMinute >= 0 && parsedMinute < 60,
                $"{warningHeader} minute should be greater or equal 0 and less than 60");
            Assert.IsTrue(parsedSecond >= 0 && parsedSecond < 60,
                $"{warningHeader} second should be greater or equal 0 and less than 60");
        }

        private static void TestPeriod(Schedule schedule, string warningHeader)
        {
            bool isPeriod = schedule.definitions.Count > 1 && schedule.definitions.Any(def => def.hour[0] != "*");

            if (schedule.definitions.Count > 0)
            {
                if (isPeriod)
                {
                    int startHour = Convert.ToInt32(schedule.definitions[0].hour[0]);
                    int endHour = Convert.ToInt32(schedule.definitions[schedule.definitions.Count - 1].hour[0]);

                    string startMinutesRange = schedule.definitions[0].minute[0];
                    string[] startSplitRange = startMinutesRange.Split('-');
                    int startMinute = Convert.ToInt32(startSplitRange[0]);

                    string endMinutesRange = schedule.definitions[schedule.definitions.Count - 1].minute[0];
                    string[] endSplitRange = endMinutesRange.Split('-');
                    int endMinute = Convert.ToInt32(endSplitRange[1]);

                    bool valid = endHour > startHour || (endHour == startHour && endMinute > startMinute);
                    Assert.IsTrue(valid, $"{warningHeader} active period to should be later than active period from");
                }
            }
        }
    }
}