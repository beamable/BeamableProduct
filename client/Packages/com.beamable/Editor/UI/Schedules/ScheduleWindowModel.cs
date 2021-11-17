using Beamable.Common.Content;
using Beamable.Editor.UI.Validation;

namespace Beamable.Editor.Schedules
{
	public abstract class ScheduleWindowModel
	{
		public enum WindowMode
		{
			Daily,
			Days,
			Dates
		}

		protected readonly ScheduleParser ScheduleParser = new ScheduleParser();

		public abstract WindowMode Mode
		{
			get;
		}

		protected ComponentsValidator Validator
		{
			get;
			set;
		}

		public abstract Schedule GetSchedule();

		public void ForceValidationCheck()
		{
			Validator.ForceValidationCheck();
		}
	}
}
