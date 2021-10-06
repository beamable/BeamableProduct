using System;
using System.Collections.Generic;
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
        public Action OnCancel;
        public Action<string> OnConfirm;

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
#endif
        public static BeamablePopupWindow OpenWindow()
        {
            EventScheduleWindow eventScheduleWindow = new EventScheduleWindow();

            return BeamablePopupWindow.ShowUtility(BeamableComponentsConstants.SCHEDULES_WINDOW_HEADER,
                eventScheduleWindow, null, BeamableComponentsConstants.EventSchedulesWindowSize);
        }

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
            Schedule newSchedule = new Schedule();

            PrepareGeneralData(newSchedule);

            switch (_currentMode)
            {
                case Mode.Daily:
                    PrepareDailyModeData(newSchedule);
                    break;
                case Mode.Days:
                    PrepareDaysModeData(newSchedule);
                    break;
                case Mode.Dates:
                    PrepareDateModeData(newSchedule);
                    break;
            }

            string json = JsonUtility.ToJson(new ScheduleWrapper(newSchedule));
            string replaced = json.Replace("\"\"", "null");

            // TODO: remove it before push
            Debug.Log(replaced);
            
            OnConfirm?.Invoke(replaced);
        }

        private void CancelClicked(MouseDownEvent evt)
        {
            OnCancel?.Invoke();
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
                // ve.SetHeight(0);
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

        #region Data parsing (to be moved to separate objects)

        private void PrepareGeneralData(Schedule newSchedule)
        {
            newSchedule.description = _descriptionComponent.Value;
            newSchedule.activeFrom = _startTimeComponent.SelectedHour;
            newSchedule.activeTo = _neverExpiresComponent.Value
                ? ""
                : $"{_activeToDateComponent.SelectedDate}:{_activeToHourComponent.SelectedHour}";
        }

        private void PrepareDailyModeData(Schedule newSchedule)
        {
            ScheduleDefinition definition =
                new ScheduleDefinition("0", "0", "0", new List<string> {"*"}, "*", "*", new List<string> {"*"});
            newSchedule.AddDefinition(definition);
        }

        private void PrepareDaysModeData(Schedule newSchedule)
        {
            ScheduleDefinition definition = new ScheduleDefinition(_startTimeComponent.Second,
                _startTimeComponent.Minute, _startTimeComponent.Hour, new List<string> {"*"}, "*", "*",
                _daysDaysPickerComponent.DaysPicker.GetSelectedDays());
            newSchedule.AddDefinition(definition);
        }

        private void PrepareDateModeData(Schedule newSchedule)
        {
            Dictionary<string, List<string>> sortedDates = ParseDates(_datesField.Value);

            foreach (KeyValuePair<string, List<string>> pair in sortedDates)
            {
                string[] monthAndYear = pair.Key.Split('-');
                string month = monthAndYear[0];
                string year = monthAndYear[1];

                List<string> daysInCurrentMonthAndYear = new List<string>();

                foreach (string dateString in pair.Value)
                {
                    string[] splitDate = dateString.Split('-');
                    string day = splitDate[0];
                    daysInCurrentMonthAndYear.Add(day);
                }

                ScheduleDefinition definition = new ScheduleDefinition(_startTimeComponent.Second,
                    _startTimeComponent.Minute, _startTimeComponent.Hour, daysInCurrentMonthAndYear, month, year,
                    new List<string> {"*"});

                newSchedule.AddDefinition(definition);
            }
        }

        private Dictionary<string, List<string>> ParseDates(string value)
        {
            // TODO: add some input string validation
            if (string.IsNullOrEmpty(value))
            {
                return new Dictionary<string, List<string>>();
            }

            Dictionary<string, List<string>> sortedDates = new Dictionary<string, List<string>>();

            string[] dates = value.Split(';');

            foreach (string date in dates)
            {
                string[] dateElements = date.Split('-');
                string month = dateElements[1];
                string year = dateElements[2];
                string monthAndYear = $"{month}-{year}";

                if (sortedDates.ContainsKey(monthAndYear))
                {
                    if (sortedDates.TryGetValue(monthAndYear, out var list))
                    {
                        list.Add(date);
                    }
                }
                else
                {
                    sortedDates.Add(monthAndYear, new List<string> {date});
                }
            }

            return sortedDates;
        }

        #endregion
    }
}