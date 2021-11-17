using System;
using System.Collections.Generic;
using Beamable.Common.Content;

namespace Beamable.Editor.Schedules
{
	[Serializable]
	public class ScheduleWrapper
	{
		public Schedule schedule;

		public ScheduleWrapper(Schedule schedule)
		{
			this.schedule = schedule;
		}
	}
}
