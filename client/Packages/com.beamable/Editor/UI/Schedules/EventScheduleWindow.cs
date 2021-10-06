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
    public class EventScheduleWindow : BeamableVisualElement
    {
        private enum Mode
        {
            Daily,
            Days,
            Dates
        }

        private LabeledTextField _eventNameComponent;
        private LabeledTextField _descriptionComponent;
        private LabeledHourPickerVisualElement _startTimeComponent;
        private LabeledDatePickerVisualElement _activeToDateComponent;
        private LabeledHourPickerVisualElement _activeToHourComponent;
        private LabeledDropdownVisualElement _dropdownComponent;
        private BeamableCheckboxVisualElement _neverExpiresComponent;
        private LabeledDaysPickerVisualElement _daysDaysPickerComponent;
        private LabeledTextField _datesField;
        private VisualElement _daysGroup;
        private VisualElement _datesGroup;
        private VisualElement _confirmButton;

        private readonly Dictionary<string, Mode> _modes;
        private Mode _currentMode;

#if BEAMABLE_DEVELOPER
        // TODO: remove it before final push
        [MenuItem("TESTING/Event schedule window")]
        public static void OpenWindow()
        {
            EventScheduleWindow eventScheduleWindow = new EventScheduleWindow();

            BeamablePopupWindow.ShowUtility(BeamableComponentsConstants.SCHEDULES_WINDOW_HEADER,
                eventScheduleWindow, null, BeamableComponentsConstants.EventSchedulesWindowSize);
        }
#endif

        public EventScheduleWindow() : base(
            $"{BeamableComponentsConstants.SCHEDULES_PATH}/{nameof(EventScheduleWindow)}")
        {
            _modes = new Dictionary<string, Mode>
            {
                {"Daily", Mode.Daily},
                {"Days of week", Mode.Days},
                {"Actual dates", Mode.Dates}
            };

            _currentMode = Mode.Daily;
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

            _neverExpiresComponent = Root.Q<BeamableCheckboxVisualElement>("expiresNever");
            _neverExpiresComponent.OnValueChanged += OnExpirationChanged;
            _neverExpiresComponent.Refresh();
            
            _activeToDateComponent = Root.Q<LabeledDatePickerVisualElement>("activeToDate");
            _activeToDateComponent.Refresh();

            _activeToHourComponent = Root.Q<LabeledHourPickerVisualElement>("activeToHour");
            _activeToHourComponent.Refresh();

            // Days mode
            _daysDaysPickerComponent = Root.Q<LabeledDaysPickerVisualElement>("daysPicker");
            _daysDaysPickerComponent.Refresh();

            // Date mode
            // TODO: add calendar component
            _datesField = Root.Q<LabeledTextField>("datesField");
            _datesField.Refresh();

            // Buttons
            _confirmButton = Root.Q<VisualElement>("confirmBtn");
            _confirmButton.RegisterCallback<MouseDownEvent>(ConfirmClicked);

            _confirmButton = Root.Q<VisualElement>("cancelBtn");
            _confirmButton.RegisterCallback<MouseDownEvent>(CancelClicked);

            // Groups
            _daysGroup = Root.Q<VisualElement>("daysGroup");
            _datesGroup = Root.Q<VisualElement>("datesGroup");

            RefreshGroups();
            OnExpirationChanged(_neverExpiresComponent.Value);
        }

        protected override void OnDestroy()
        {
            if (_neverExpiresComponent != null)
            {
                _neverExpiresComponent.OnValueChanged -= OnExpirationChanged;
            }
        }

        private void OnExpirationChanged(bool value)
        {
            _activeToDateComponent.SetEnabled(!value);
            _activeToHourComponent.SetEnabled(!value);
        }

        private void ConfirmClicked(MouseDownEvent evt)
        {
            StringBuilder builder = new StringBuilder();
            PrepareGeneralData(builder);

            switch (_currentMode)
            {
                case Mode.Days:
                    PrepareDaysModeData(builder);
                    break;
                case Mode.Dates:
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
            builder.Append($"Never expires: {_neverExpiresComponent.Value}\n");

            if (_neverExpiresComponent.Value)
            {
                return;
            }

            builder.Append($"Active to date: {_activeToDateComponent.SelectedDate}\n");
            builder.Append($"Active to hour: {_activeToHourComponent.SelectedHour}\n");
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

            builder.Append($"Active to date: {_activeToDateComponent.SelectedDate}\n");
            builder.Append($"Active to hour: {_activeToHourComponent.SelectedHour}\n");
        }

        private void PrepareDateModeData(StringBuilder builder)
        {
            builder.Append($"Repeat on: {_datesField.Value}\n");
        }

        private void CancelClicked(MouseDownEvent evt)
        {
            Debug.Log("Close");
        }

        private void RefreshGroups()
        {
            RefreshSingleGroup(_daysGroup, Mode.Days);
            RefreshSingleGroup(_datesGroup, Mode.Dates);
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