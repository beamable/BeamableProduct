using Beamable.Common.Content;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Validation;
using System;

namespace Beamable.Editor.Models.Schedules
{
	public class ListingDailyScheduleModel : ScheduleWindowModel
	{
		private readonly LabeledTextField _descriptionComponent;
		private readonly LabeledCheckboxVisualElement _neverExpiresComponent;
		private readonly LabeledDatePickerVisualElement _activeToDateComponent;
		private readonly LabeledHourPickerVisualElement _activeToHourComponent;
		private readonly LabeledCheckboxVisualElement _allDayComponent;
		private readonly LabeledHourPickerVisualElement _periodFromHourComponent;
		private readonly LabeledHourPickerVisualElement _periodToHourComponent;

		public ListingDailyScheduleModel(LabeledTextField descriptionComponent,
			LabeledCheckboxVisualElement neverExpiresComponent,
			LabeledDatePickerVisualElement activeToDateComponent,
			LabeledHourPickerVisualElement activeToHourComponent,
			LabeledCheckboxVisualElement allDayComponent,
			LabeledHourPickerVisualElement periodFromHourComponent,
			LabeledHourPickerVisualElement periodToHourComponent,
			Action<bool, string> refreshConfirmButton)
		{
			_descriptionComponent = descriptionComponent;
			_neverExpiresComponent = neverExpiresComponent;
			_activeToDateComponent = activeToDateComponent;
			_activeToHourComponent = activeToHourComponent;
			_allDayComponent = allDayComponent;
			_periodFromHourComponent = periodFromHourComponent;
			_periodToHourComponent = periodToHourComponent;

			Validator = new ComponentsValidator(refreshConfirmButton);
		}

		public override WindowMode Mode => WindowMode.Daily;

		public override Schedule GetSchedule()
		{
			Schedule newSchedule = new Schedule();

			ScheduleParser.PrepareGeneralData(newSchedule, _descriptionComponent.Value, _neverExpiresComponent.Value,
				$"{_activeToDateComponent.SelectedDate}{_activeToHourComponent.SelectedHour}");

			if (!_allDayComponent.Value)
			{
				var fromHour = int.Parse(_periodFromHourComponent.Hour);
				var toHour = int.Parse(_periodToHourComponent.Hour);
				var fromMinute = int.Parse(_periodFromHourComponent.Minute);
				var toMinute = int.Parse(_periodToHourComponent.Minute);
				ScheduleParser.PrepareListingDailyModeData(newSchedule, fromHour, toHour, fromMinute, toMinute);
			}
			else
			{
				ScheduleParser.PrepareListingDailyModeData(newSchedule, 0, 0, 0, 0);
			}
			
			return newSchedule;
		}
	}
}
