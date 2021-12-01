using System.Collections.Generic;
using Beamable.Common.Content;
using System;
using System.Linq;
using UnityEngine;

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
	        var scheduleDateModeModels = ParseDates(selectedDays);
	        foreach (var model in scheduleDateModeModels)
	        {
		        var definition = new ScheduleDefinition(
			        new List<string> {second},
			        new List<string> {minute}, 
			        new List<string> {hour}, 
			        model.Days, 
			        model.Months, 
			        model.Years, 
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
	        var scheduleDateModeModels = ParseDates(selectedDates);
            var periodsSchedulesDefinitions = GetPeriodsSchedulesDefinitions(fromHour, toHour, fromMinute, toMinute, new List<string> {"*"});

            foreach (var model in scheduleDateModeModels)
            {
	            foreach (var scheduleDefinition in periodsSchedulesDefinitions)
	            {
		            var definition = new ScheduleDefinition(
			            new List<string>{"*"},
			            scheduleDefinition.minute, 
			            scheduleDefinition.hour,
			            model.Days, 
			            model.Months,
			            model.Years,
			            new List<string> {"*"});
		            newSchedule.AddDefinition(definition);
	            }
            }
        }

        private List<ScheduleDefinition> GetPeriodsSchedulesDefinitions(int fromHour, int toHour, int fromMinute,
            int toMinute, List<string> selectedDays)
        {
            int hoursDelta = toHour - fromHour;
            var definitions = new List<ScheduleDefinition>();

            if (hoursDelta == 0)
            {
                var definition = new ScheduleDefinition(
	                new List<string>{"*"}, 
	                ConvertIntoRangeList(fromMinute, toMinute),
	                new List<string> {$"{fromHour}"}, 
	                new List<string> {"*"}, 
	                new List<string> {"*"}, 
	                new List<string> {"*"}, 
	                selectedDays);
                definitions.Add(definition);
            }
            else if (hoursDelta == 1)
            {
	            var definition = new ScheduleDefinition(
		            new List<string>{"*"}, 
		            ConvertIntoRangeList(fromMinute, toMinute), 
		            new List<string> {$"{fromHour}"}, 
		            new List<string> {"*"}, 
		            new List<string> {"*"}, 
		            new List<string> {"*"}, 
		            selectedDays);
	            definitions.Add(definition);
	            
                var startDefinition = new ScheduleDefinition(
	                new List<string>{"*"},  
	                ConvertIntoRangeList(fromMinute, 59),
	                new List<string> {$"{fromHour}"},  
	                new List<string> {"*"}, 
	                new List<string>{"*"}, 
	                new List<string>{"*"}, 
	                selectedDays);
                definitions.Add(startDefinition);

                var endDefinition = new ScheduleDefinition(
	                new List<string>{"*"}, 
	                ConvertIntoRangeList(0, toMinute),
	                new List<string> {$"{toHour}"}, 
	                new List<string> {"*"}, 
	                new List<string>{"*"}, 
	                new List<string>{"*"}, 
	                selectedDays);
                definitions.Add(endDefinition);
            }
            else
            {
                var startDefinition = new ScheduleDefinition(
	                new List<string>{"*"}, 
	                ConvertIntoRangeList(fromMinute, 59),
	                new List<string> {$"{fromHour}"}, 
	                new List<string> {"*"}, 
	                new List<string>{"*"}, 
	                new List<string>{"*"}, 
	                selectedDays);
                definitions.Add(startDefinition);

                var middleDefinition = new ScheduleDefinition(
	                new List<string> {"*"},
	                new List<string> {"*"},
	                hoursDelta == 2 ? new List<string> {$"{fromHour+1}"} : ConvertIntoRangeList(fromHour + 1, toHour - 1),
					new List<string> {"*"}, 
					new List<string>{"*"},
					new List<string>{"*"}, 
					selectedDays);

                var endDefinition = new ScheduleDefinition(
	                new List<string>{"*"}, 
	                ConvertIntoRangeList(0, toMinute),
	                new List<string> {$"{toHour}"}, 
	                new List<string> {"*"}, 
	                new List<string>{"*"}, 
	                new List<string>{"*"}, 
	                selectedDays);
                definitions.Add(endDefinition);
            }

            return definitions;
            
            List<string> ConvertIntoRangeList(int from, int to)
            {
	            var tempList = new List<string>();
	            for (int i = from; i <= to; i++)
		            tempList.Add($"{i}");
	            return tempList;
            }
        }

        private List<ScheduleDateModeModel> ParseDates(List<string> dates)
        {
	        var monthYearKeyDates =  PrepareMonthYearKeys();
	        SortDays();
            var groupsBasedOnDays = CreateGroupsBasedOnDays();
            var tempModels = CreateDateModels();
            
            return tempModels;

            Dictionary<string, string> PrepareMonthYearKeys()
            {
	            var dict = new Dictionary<string, string>();
	            foreach (string date in dates)
	            {
		            string[] dateElements = date.Split('-');
		            string day = dateElements[0];
		            string month = dateElements[1];
		            string year = dateElements[2];
		            string monthAndYear = $"{month}-{year}";

		            if (dict.ContainsKey(monthAndYear))
			            dict[monthAndYear] += $",{day}";
		            else
			            dict.Add(monthAndYear, $"{day}");
	            }
	            return dict;
            }
            void SortDays()
            {
	            foreach (var sortedDate in monthYearKeyDates.ToList())
		            monthYearKeyDates[sortedDate.Key] = String.Join(",", sortedDate.Value.Split(',').OrderBy(q => q).ToArray());
            }
            Dictionary<string, Dictionary<string, string>> CreateGroupsBasedOnDays()
            {
	            var dict = new Dictionary<string, Dictionary<string, string>>();
	            foreach (var sortedDate in monthYearKeyDates)
	            {
		            var splittedKey = sortedDate.Key.Split('-');
		            var month = splittedKey[0];
		            var year = splittedKey[1];

		            if (dict.ContainsKey(sortedDate.Value))
		            {
			            if (dict[sortedDate.Value].ContainsKey(year))
			            {
				            dict[sortedDate.Value][year] += $"-{month}";
			            }
			            else
			            {
				            dict[sortedDate.Value].Add(year, month);
			            }
		            }
		            else
		            {
			            dict.Add(sortedDate.Value, new Dictionary<string, string> {{year, month}});
		            }
	            }
	            return dict;
            }
            List<ScheduleDateModeModel> CreateDateModels()
            {
	            var models = new List<ScheduleDateModeModel>();
	            foreach (var kvp in groupsBasedOnDays)
	            {
		            foreach (var kvp2 in kvp.Value)
		            {
			            var days = kvp.Key.Split(',').ToList();
			            var months = kvp2.Value.Split('-').ToList();
			            var years = new List<string> {kvp2.Key};
			            models.Add(new ScheduleDateModeModel(days, months, years));
		            }
	            }
	            return models;
            }
        }
    }

    public class ScheduleDateModeModel
    {
	    public List<string> Days { get; }
	    public List<string> Months { get; }
	    public List<string> Years { get; }

	    public ScheduleDateModeModel(IEnumerable<string> days, IEnumerable<string> months, IEnumerable<string> years)
	    {
		    Days = days.OrderBy(x => x).ToList();
		    Months = months.OrderBy(x => x).ToList();
		    Years = years.OrderBy(x => x).ToList();
	    }
    }
}
