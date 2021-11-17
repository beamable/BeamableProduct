using Beamable.Common.Content;
using System;
using System.Collections.Generic;

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
