using System;
using Beamable.Common.Content;

namespace Beamable.Editor.Schedules
{
   public interface IScheduleWindow
   {
      event Action<Schedule> OnScheduleUpdated;
   }
}