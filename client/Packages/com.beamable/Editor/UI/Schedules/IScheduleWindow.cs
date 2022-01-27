using Beamable.Common.Content;
using System;

namespace Beamable.Editor.Schedules
{
	public interface IScheduleWindow<TData>
	{
		event Action OnCancelled;
		event Action<Schedule> OnScheduleUpdated;
		void Set(Schedule schedule, TData data);
		void ApplyDataTransforms(TData data);
	}
}
