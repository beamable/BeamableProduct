using System.Collections.Generic;
using System.Text;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Schedules
{
    public class ScheduleWindow : BeamableVisualElement
    {
        public enum Mode
        {
            DAILY,
            DAYS,
            DATES
        }

        private LabeledTextField _eventNameComponent;
        private LabeledTextField _descriptionComponent;
        private LabeledHourPickerVisualElement _startTimeComponent;
        private LabeledDatePickerVisualElement _dailyActiveToDateComponent;
        private LabeledHourPickerVisualElement _dailyActiveToHourComponent;
        private LabeledDropdownVisualElement _dropdownComponent;
        private LabeledDatePickerVisualElement _daysActiveToDateComponent;
        private LabeledHourPickerVisualElement _daysActiveToHourComponent;
        private LabeledDaysPickerVisualElement _daysDaysPickerComponent;
        private LabeledDatePickerVisualElement _dateActiveToDateComponent;
        private LabeledHourPickerVisualElement _dateActiveToHourComponent;

        private readonly Dictionary<string, Mode> _modes;
        private Mode _currentMode;
        private VisualElement _dailyGroup;
        private VisualElement _daysGroup;
        private VisualElement _datesGroup;
        private VisualElement _confirmButton;

        // TODO: remove it before final push
        [MenuItem("TESTING/Schedule window")]
        public static void OpenWindow()
        {
            ScheduleWindow scheduleWindow = new ScheduleWindow();

            BeamablePopupWindow.ShowUtility(BeamableComponentsConstants.SCHEDULES_WINDOW_HEADER,
                scheduleWindow, null, BeamableComponentsConstants.SchedulesWindowSize);
        }

        public ScheduleWindow() : base(
            $"{BeamableComponentsConstants.UI_PACKAGE_PATH}/Schedules/{nameof(ScheduleWindow)}")
        {
            _modes = new Dictionary<string, Mode>
            {
                {"Daily", Mode.DAILY},
                {"Days of week", Mode.DAYS},
                {"Actual dates", Mode.DATES}
            };

            _currentMode = Mode.DAILY;
        }

        public override void Refresh()
        {
            base.Refresh();

            _eventNameComponent = Root.Q<LabeledTextField>("eventName");
            _eventNameComponent.Refresh();

            _descriptionComponent = Root.Q<LabeledTextField>("description");
            _descriptionComponent.Refresh();

            _startTimeComponent = Root.Q<LabeledHourPickerVisualElement>("startTime");
            _startTimeComponent.Refresh();

            _dropdownComponent = Root.Q<LabeledDropdownVisualElement>("dropdown");
            _dropdownComponent.Setup(PrepareOptions(), OnModeChanged);
            _dropdownComponent.Refresh();
            
            // Daily mode
            _dailyActiveToDateComponent = Root.Q<LabeledDatePickerVisualElement>("daily_activeToDate");
            _dailyActiveToDateComponent.Refresh();

            _dailyActiveToHourComponent = Root.Q<LabeledHourPickerVisualElement>("daily_activeToHour");
            _dailyActiveToHourComponent.Refresh();

            // Days mode
            _daysDaysPickerComponent = Root.Q<LabeledDaysPickerVisualElement>("days_daysPicker");
            _daysDaysPickerComponent.Refresh();

            _daysActiveToDateComponent = Root.Q<LabeledDatePickerVisualElement>("days_activeToDate");
            _daysActiveToDateComponent.Refresh();

            _daysActiveToHourComponent = Root.Q<LabeledHourPickerVisualElement>("days_activeToHour");
            _daysActiveToHourComponent.Refresh();

            // Date mode
            _dateActiveToDateComponent = Root.Q<LabeledDatePickerVisualElement>("date_activeToDate");
            _dateActiveToDateComponent.Refresh();

            _dateActiveToHourComponent = Root.Q<LabeledHourPickerVisualElement>("date_activeToHour");
            _dateActiveToHourComponent.Refresh();

            // Buttons
            _confirmButton = Root.Q<VisualElement>("confirmBtn");
            _confirmButton.RegisterCallback<MouseDownEvent>(ConfirmClicked);

            _confirmButton = Root.Q<VisualElement>("cancelBtn");
            _confirmButton.RegisterCallback<MouseDownEvent>(CancelClicked);

            // Groups
            _dailyGroup = Root.Q<VisualElement>("dailyGroup");
            _daysGroup = Root.Q<VisualElement>("daysGroup");
            _datesGroup = Root.Q<VisualElement>("datesGroup");

            RefreshGroups();
        }

        private void ConfirmClicked(MouseDownEvent evt)
        {
            StringBuilder builder = new StringBuilder();
            PrepareGeneralData(builder);
            
            switch (_currentMode)
            {
                case Mode.DAILY:
                    PrepareDailyModeData(builder);
                    break;
                case Mode.DAYS:
                    PrepareDaysModeData(builder);
                    break;
                case Mode.DATES:
                    PrepareDateModeData(builder);
                    break;
            }
            
            Debug.Log(builder.ToString());
            //TODO: return data and close window
        }

        private void PrepareGeneralData(StringBuilder builder)
        {
            builder.Append($"Event name: {_eventNameComponent.Value}\n");
            builder.Append($"Start time: {_startTimeComponent.SelectedHour}\n");
            builder.Append($"Event description: {_descriptionComponent.Value}\n");
        }

        private void PrepareDailyModeData(StringBuilder builder)
        {
            builder.Append($"Active to date: {_dailyActiveToDateComponent.SelectedDate}\n");
            builder.Append($"Active to hour: {_dailyActiveToHourComponent.SelectedHour}\n");
        }

        private void PrepareDaysModeData(StringBuilder builder)
        {
            builder.Append("Selected days: ");
            List<string> selectedDays = _daysDaysPickerComponent.DaysPicker.GetSelectedDays();

            for (var index = 0; index < selectedDays.Count; index++)
            {
                string day = selectedDays[index];
                builder.Append(index < selectedDays.Count - 1 ? $"{day.ToUpper()}, " : $"{day.ToUpper()}\n");
            }
            
            builder.Append($"Active to date: {_daysActiveToDateComponent.SelectedDate}\n");
            builder.Append($"Active to hour: {_daysActiveToHourComponent.SelectedHour}\n");
        }

        private void PrepareDateModeData(StringBuilder builder)
        {
            builder.Append($"Active to date: {_dateActiveToDateComponent.SelectedDate}\n");
            builder.Append($"Active to hour: {_dateActiveToHourComponent.SelectedHour}\n");
        }

        private void CancelClicked(MouseDownEvent evt)
        {
            Debug.Log("Close");
        }

        private void RefreshGroups()
        {
            RefreshSingleGroup(_dailyGroup, Mode.DAILY);
            RefreshSingleGroup(_daysGroup, Mode.DAYS);
            RefreshSingleGroup(_datesGroup, Mode.DATES);
        }

        private void RefreshSingleGroup(VisualElement ve, Mode mode)
        {
            if (ve != null)
            {
                ve.visible = _currentMode == mode;
                ve.style.height = ve.visible ? new StyleLength(StyleKeyword.Auto) : new StyleLength(0.0f);
            }
        }

        private void OnModeChanged(string option)
        {
            if (_modes.TryGetValue(option, out Mode newMode))
            {
                _currentMode = newMode;
            }

            RefreshGroups();
        }

        private List<string> PrepareOptions()
        {
            List<string> options = new List<string>();

            foreach (KeyValuePair<string, Mode> pair in _modes)
            {
                options.Add(pair.Key);
            }

            return options;
        }
    }
}