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

        public void PrepareDailyModeData(Schedule newSchedule, string second, string minute, string hour)
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

        public void PrepareDateModeData(Schedule newSchedule, List<string> selectedDays, string hour, string minute,
            string second)
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