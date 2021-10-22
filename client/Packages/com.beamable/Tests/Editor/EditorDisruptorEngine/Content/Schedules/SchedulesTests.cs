using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Beamable.Common.Content;
using NUnit.Framework;

namespace Beamable.Editor.Tests.Content
{
    public class SchedulesTests
    {
        [Test]
        public void Event_Schedule_Daily_Mode_Test()
        {
            String warningHeader = "Daily event schedule:";

            Schedule schedule = new Schedule();
            schedule.activeFrom = "2021-10-07T12:15:15Z";

            ScheduleDefinition definition = new ScheduleDefinition("0", "0", "0", new List<string> {"*"}, "*", "*",
                new List<string> {"*"});

            schedule.AddDefinition(definition);

            bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);
            Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
            Assert.IsTrue(schedule.definitions.Count <= 1, $"{warningHeader} should have only one definition");
        }

        [Test]
        public void Event_Schedule_Days_Mode_Test()
        {
            String warningHeader = "Days event schedule:";

            Schedule schedule = new Schedule();
            schedule.activeFrom = "2021-10-07T12:15:15Z";
            bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime parsedActiveFrom);

            ScheduleDefinition definition = new ScheduleDefinition(parsedActiveFrom.Second.ToString(),
                parsedActiveFrom.Minute.ToString(), parsedActiveFrom.Hour.ToString(), new List<string> {"*"}, "*", "*",
                new List<string> {"1", "3", "5"});

            schedule.AddDefinition(definition);

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

        [Test]
        public void Event_Schedule_Dates_Mode_Test()
        {
            String warningHeader = "Dates event schedule:";

            Schedule schedule = new Schedule();
            schedule.activeFrom = "2021-10-07T12:15:15Z";
            bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime parsedActiveFrom);

            ScheduleDefinition definition = new ScheduleDefinition(parsedActiveFrom.Second.ToString(),
                parsedActiveFrom.Minute.ToString(), parsedActiveFrom.Hour.ToString(), new List<string> {"01", "03"},
                "10", "2021", new List<string> {"*"});

            schedule.AddDefinition(definition);

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

        [Test]
        public void Listing_Schedule_Daily_Mode_Test()
        {
            String warningHeader = "Daily listing schedule:";

            Schedule schedule = new Schedule();
            schedule.activeFrom = "2021-10-07T12:15:15Z";

            ScheduleDefinition definition = new ScheduleDefinition("*", "0-59", "10", new List<string> {"*"}, "*", "*",
                new List<string> {"*"});

            schedule.AddDefinition(definition);

            Assert.IsTrue(DateTime.TryParse(schedule.activeFrom, out DateTime _),
                $"{warningHeader} problem with parsing activeFrom field");
            Assert.IsTrue(schedule.definitions.Count > 0 && schedule.definitions.Count <= 3,
                $"{warningHeader} definitions amount should be greater than 0 and less or equal to 3");

            foreach (ScheduleDefinition scheduleDefinition in schedule.definitions)
            {
                string minuteString = scheduleDefinition.minute[0];
                string hoursString = scheduleDefinition.hour[0];
                bool minutesMatchPattern = Regex.IsMatch(minuteString, "[0-59]-[0-59]");
                bool hoursMatchPattern =
                    Regex.IsMatch(hoursString, "[0-23]-[0-23]") || Regex.IsMatch(hoursString, "[0-23]");

                Assert.IsTrue(minuteString != "*", $"{warningHeader} should have minutes range passed");
                Assert.IsTrue(minutesMatchPattern, $"{warningHeader} minutes doesn't match pattern");
                Assert.IsTrue(hoursString != "*", $"{warningHeader} should have hour value or range passed");
                Assert.IsTrue(hoursMatchPattern, $"{warningHeader} hours doesn't match pattern");
            }
        }

        [Test]
        public void Listing_Schedule_Days_Mode_Test()
        {
            String warningHeader = "Days listing schedule:";

            Schedule schedule = new Schedule();
            schedule.activeFrom = "2021-10-07T12:15:15Z";
            bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);

            ScheduleDefinition definition = new ScheduleDefinition("*", "*", "*", new List<string> {"*"}, "*", "*",
                new List<string> {"1", "3", "5"});

            schedule.AddDefinition(definition);

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

        [Test]
        public void Listing_Schedule_Dates_Mode_Test()
        {
            String warningHeader = "Dates event schedule:";

            Schedule schedule = new Schedule();
            schedule.activeFrom = "2021-10-07T12:15:15Z";
            bool parsedDateTime = DateTime.TryParse(schedule.activeFrom, out DateTime _);

            ScheduleDefinition definition = new ScheduleDefinition("*", "*", "*", new List<string> {"01", "03"},
                "10", "2021", new List<string> {"*"});

            schedule.AddDefinition(definition);

            Assert.IsTrue(parsedDateTime, $"{warningHeader} problem with parsing activeFrom field");
            Assert.IsTrue(schedule.definitions.Count > 0 && schedule.definitions.Count <= 3,
                $"{warningHeader} definitions amount should be greater than 0 and less or equal to 3");
            
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