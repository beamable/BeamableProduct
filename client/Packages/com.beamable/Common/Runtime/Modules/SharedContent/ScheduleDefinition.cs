using System;
using System.Collections.Generic;
using Beamable.Common.Content.Validation;
using UnityEngine;

namespace Beamable.Common.Content
{
    [Serializable]
   public class OptionalSchedule : Optional<Schedule> {}

   [Serializable]
   public class OptionalListingSchedule : Optional<ListingSchedule> {}

   [Serializable]
   public class OptionalEventSchedule : Optional<EventSchedule> {}

   [Serializable]
   public class EventSchedule : Schedule {}

   [Serializable]
   public class ListingSchedule : Schedule {}

   [Serializable]
   public class Schedule
   {
      public string description;

      [MustBeDateString]
      public string activeFrom;

      [MustBeDateString]
      public OptionalString activeTo = new OptionalString();

      public List<ScheduleDefinition> definitions = new List<ScheduleDefinition>();

      public void AddDefinition(ScheduleDefinition definition)
      {
         definitions.Add(definition);
      }

      public void AddDefinitions(List<ScheduleDefinition> definitions)
      {
         this.definitions.AddRange(definitions);
      }
   }

   [Serializable]
   public class ScheduleDefinition
   {
       public Action<ScheduleDefinition> OnCronRawSaveButtonPressed;
       [HideInInspector]
       public int index = -1;
       
       [ShowOnly] public string cronHumanFormat;
       [ShowOnly] public string cronRawFormat;
       
      public List<string> second;
      public List<string> minute;
      public List<string> hour;
      public List<string> dayOfMonth;
      public List<string> month;
      public List<string> year;
      public List<string> dayOfWeek;

      public ScheduleDefinition() { }

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