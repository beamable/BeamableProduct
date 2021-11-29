using System.Collections.Generic;
using Beamable.Common.Content;

namespace Beamable.Editor.Schedules
{
    public class ScheduleParser
    {
        public void PrepareGeneralData(Schedule newSchedule, string description, string activeFrom, bool expires,
            string activeTo)
        {
            newSchedule.description = description;
            newSchedule.activeFrom = activeFrom;
            newSchedule.activeTo.HasValue = !expires;
            newSchedule.activeTo.Value = activeTo;
        }

        public void PrepareDailyModeData(Schedule newSchedule, string hour, string minute, string second)
        {
            ScheduleDefinition definition =
                new ScheduleDefinition(second, minute, hour, new List<string> {"*"}, "*", "*", new List<string> {"*"});
            newSchedule.AddDefinition(definition);
        }

        public void PrepareDaysModeData(Schedule newSchedule, string hour, string minute, string second,
            List<string> selectedDays)
        {
            ScheduleDefinition definition = new ScheduleDefinition(second,
                minute, hour, new List<string> {"*"}, "*", "*",
                selectedDays);
            newSchedule.AddDefinition(definition);
        }

        public void PrepareDateModeData(Schedule newSchedule, string hour, string minute, string second,
            List<string> selectedDays)
        {
            Dictionary<string, List<string>> sortedDates = ParseDates(selectedDays);

            foreach (KeyValuePair<string, List<string>> pair in sortedDates)
            {
                string[] monthAndYear = pair.Key.Split('-');
                string month = monthAndYear[0];
                string year = monthAndYear[1];

                List<string> daysInCurrentMonthAndYear = new List<string>();

                foreach (string dateString in pair.Value)
                {
                    string[] splitDate = dateString.Split('-');
                    string day = splitDate[0];
                    daysInCurrentMonthAndYear.Add(day);
                }

                ScheduleDefinition definition = new ScheduleDefinition(second,
                    minute, hour, daysInCurrentMonthAndYear, month, year,
                    new List<string> {"*"});

                newSchedule.AddDefinition(definition);
            }
        }

        public void PrepareListingDailyModeData(Schedule newSchedule, int fromHour, int toHour, int fromMinute,
            int toMinute)
        {
            List<ScheduleDefinition> definitions = GetPeriodsSchedulesDefinitions(fromHour, toHour, fromMinute,
                toMinute,
                new List<string> {"*"});
            newSchedule.AddDefinitions(definitions);
        }

        public void PrepareListingDaysModeData(Schedule newSchedule, int fromHour, int toHour, int fromMinute,
            int toMinute, List<string> selectedDays)
        {
            List<ScheduleDefinition> definitions = GetPeriodsSchedulesDefinitions(fromHour, toHour, fromMinute,
                toMinute,
                selectedDays);
            newSchedule.AddDefinitions(definitions);
        }

        public void PrepareListingDatesModeData(Schedule newSchedule, int fromHour, int toHour, int fromMinute,
            int toMinute, List<string> selectedDates)
        {
            Dictionary<string, List<string>> sortedDates = ParseDates(selectedDates);
            List<ScheduleDefinition> periodsSchedulesDefinitions =
                GetPeriodsSchedulesDefinitions(fromHour, toHour, fromMinute, toMinute, new List<string> {"*"});

            foreach (KeyValuePair<string, List<string>> pair in sortedDates)
            {
                foreach (ScheduleDefinition scheduleDefinition in periodsSchedulesDefinitions)
                {
                    string[] monthAndYear = pair.Key.Split('-');
                    string month = monthAndYear[0];
                    string year = monthAndYear[1];

                    List<string> daysInCurrentMonthAndYear = new List<string>();

                    foreach (string dateString in pair.Value)
                    {
                        string[] splitDate = dateString.Split('-');
                        string day = splitDate[0];
                        daysInCurrentMonthAndYear.Add(day);
                    }

                    ScheduleDefinition definition = new ScheduleDefinition("*",
                        scheduleDefinition.minute[0], scheduleDefinition.hour[0], daysInCurrentMonthAndYear, month,
                        year,
                        new List<string> {"*"});

                    newSchedule.AddDefinition(definition);
                }
            }
        }

        private List<ScheduleDefinition> GetPeriodsSchedulesDefinitions(int fromHour, int toHour, int fromMinute,
            int toMinute, List<string> selectedDays)
        {
            int hoursDelta = toHour - fromHour;

            List<ScheduleDefinition> definitions = new List<ScheduleDefinition>();

            if (hoursDelta == 0)
            {
                ScheduleDefinition definition = new ScheduleDefinition("*", $"{fromMinute}-{toMinute}",
                    fromHour.ToString(), new List<string> {"*"}, "*", "*", selectedDays);
                definitions.Add(definition);
            }
            else if (hoursDelta == 1)
            {
                ScheduleDefinition startDefinition = new ScheduleDefinition("*", $"{fromMinute}-59",
                    fromHour.ToString(), new List<string> {"*"}, "*", "*", selectedDays);
                definitions.Add(startDefinition);

                ScheduleDefinition endDefinition = new ScheduleDefinition("*", $"0-{toMinute}",
                    toHour.ToString(), new List<string> {"*"}, "*", "*", selectedDays);
                definitions.Add(endDefinition);
            }
            else
            {
                ScheduleDefinition startDefinition = new ScheduleDefinition("*", $"{fromMinute}-59",
                    fromHour.ToString(), new List<string> {"*"}, "*", "*", selectedDays);
                definitions.Add(startDefinition);

                ScheduleDefinition middleDefinition = new ScheduleDefinition("*", $"*",
                    hoursDelta == 2 ? $"{fromHour + 1}" : $"{fromHour + 1}-{toHour - 1}", new List<string> {"*"}, "*",
                    "*", selectedDays);
                definitions.Add(middleDefinition);

                ScheduleDefinition endDefinition = new ScheduleDefinition("*", $"0-{toMinute}",
                    toHour.ToString(), new List<string> {"*"}, "*", "*", selectedDays);
                definitions.Add(endDefinition);
            }

            return definitions;
        }

        private Dictionary<string, List<string>> ParseDates(List<string> dates)
        {
            Dictionary<string, List<string>> sortedDates = new Dictionary<string, List<string>>();

            foreach (string date in dates)
            {
                string[] dateElements = date.Split('-');
                string month = dateElements[1];
                string year = dateElements[2];
                string monthAndYear = $"{month}-{year}";

                if (sortedDates.ContainsKey(monthAndYear))
                {
                    if (sortedDates.TryGetValue(monthAndYear, out var list))
                    {
                        list.Add(date);
                    }
                }
                else
                {
                    sortedDates.Add(monthAndYear, new List<string> {date});
                }
            }

            return sortedDates;
        }
    }
}
