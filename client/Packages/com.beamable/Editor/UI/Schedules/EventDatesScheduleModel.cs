using Beamable.Common.Content;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Validation;
using System;

namespace Beamable.Editor.Models.Schedules
{
	public class EventDatesScheduleModel : ScheduleWindowModel
	{
		private readonly LabeledTextField _descriptionComponent;
		private readonly LabeledHourPickerVisualElement _startTimeComponent;
		private readonly LabeledCalendarVisualElement _calendarComponent;
		private readonly LabeledCheckboxVisualElement _neverExpiresComponent;
		private readonly LabeledDatePickerVisualElement _activeToDateComponent;
		private readonly LabeledHourPickerVisualElement _activeToHourComponent;

		public override WindowMode Mode => WindowMode.Dates;

		public EventDatesScheduleModel(LabeledTextField descriptionComponent,
			LabeledHourPickerVisualElement startTimeComponent,
			LabeledCalendarVisualElement calendarComponent, LabeledCheckboxVisualElement neverExpiresComponent,
			LabeledDatePickerVisualElement activeToDateComponent, LabeledHourPickerVisualElement activeToHourComponent,
			Action<bool, string> refreshConfirmButtonCallback)
		{
			_descriptionComponent = descriptionComponent;
			_startTimeComponent = startTimeComponent;
			_calendarComponent = calendarComponent;
			_neverExpiresComponent = neverExpiresComponent;
			_activeToDateComponent = activeToDateComponent;
			_activeToHourComponent = activeToHourComponent;

			Validator = new ComponentsValidator(refreshConfirmButtonCallback);
			Validator.RegisterRule(new AtLeastOneDaySelectedRule(_calendarComponent.Label),
				_calendarComponent);
		}

		public override Schedule GetSchedule()
		{
			Schedule newSchedule = new Schedule();

			ScheduleParser.PrepareGeneralData(newSchedule, _descriptionComponent.Value,
				_startTimeComponent.SelectedHour, _neverExpiresComponent.Value,
				$"{_activeToDateComponent.SelectedDate}{_activeToHourComponent.SelectedHour}");
			ScheduleParser.PrepareDateModeData(newSchedule, _startTimeComponent.Hour, _startTimeComponent.Minute,
				_startTimeComponent.Second,
				_calendarComponent.SelectedDays);

			return newSchedule;
		}
	}
}
