using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Beamable.Editor.Schedules
{
    [Serializable]
    public class Schedule
    {
        public string description;
        public string activeFrom;
        [CanBeNull] public string activeTo;
        public List<ScheduleDefinition> definitions = new List<ScheduleDefinition>();

        public void AddDefinition(ScheduleDefinition definition)
        {
            definitions.Add(definition);
        }
    }

    [Serializable]
    public class ScheduleDefinition
    {
        public List<string> second;
        public List<string> minute;
        public List<string> hour;
        public List<string> dayOfMonth;
        public List<string> month;
        public List<string> year;
        public List<string> dayOfWeek;

        public ScheduleDefinition(string second, string minute, string hour, List<string> dayOfMonth, string month,
            string year, List<string> dayOfWeek)
        {
            this.second = new List<string> {second};
            this.minute = new List<string> {minute};
            this.hour = new List<string> {hour};
            this.dayOfMonth = new List<string>(dayOfMonth);
            this.month = new List<string> {month};
            this.year = new List<string> {year};
            this.dayOfWeek = new List<string>(dayOfWeek);
        }
    }
}