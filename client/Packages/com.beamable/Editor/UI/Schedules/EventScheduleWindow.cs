using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Content;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Editor.UI.Validation;
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
    public class EventScheduleWindow : BeamableVisualElement, IScheduleWindow<EventContent>
    {
        public event Action OnCancelled;
        public event Action<Schedule> OnScheduleUpdated;

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
        private LabeledDaysPickerVisualElement _daysPickerComponent;
        private VisualElement _daysGroup;
        private VisualElement _datesGroup;
        private PrimaryButtonVisualElement _confirmButton;
        private Button _cancelButton;

        private readonly Dictionary<string, Mode> _modes;
        private Mode _currentMode;

        private ComponentsValidator _currentValidator;
        private ComponentsValidator _dailyModeValidator;
        private ComponentsValidator _daysModeValidator;
        private ComponentsValidator _datesModeValidator;
        private LabeledCalendarVisualElement _calendarComponent;
        private readonly ScheduleParser _scheduleParser;

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
            _scheduleParser = new ScheduleParser();
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
            _daysPickerComponent = Root.Q<LabeledDaysPickerVisualElement>("daysPicker");
            _daysPickerComponent.Refresh();

            // Date mode
            _calendarComponent = Root.Q<LabeledCalendarVisualElement>("calendar");
            _calendarComponent.Refresh();

            // Buttons
            _confirmButton = Root.Q<PrimaryButtonVisualElement>("confirmBtn");
            _confirmButton.Button.clickable.clicked += ConfirmClicked;
            _confirmButton.Disable();

            _cancelButton = Root.Q<Button>("cancelBtn");
            _cancelButton.clickable.clicked += CancelClicked;

            // Groups
            _daysGroup = Root.Q<VisualElement>("daysGroup");
            _datesGroup = Root.Q<VisualElement>("datesGroup");

            RefreshGroups();
            OnExpirationChanged(_neverExpiresComponent.Value);

            _dailyModeValidator = new ComponentsValidator(RefreshConfirmButton);

            _daysModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _daysModeValidator.RegisterRuleForComponent(new AtLeastOneOptionSelectedRule(_daysPickerComponent.Label),
                _daysPickerComponent);

            _datesModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _datesModeValidator.RegisterRuleForComponent(new AtLeastOneOptionSelectedRule(_calendarComponent.Label),
                _calendarComponent);

            _currentValidator = _dailyModeValidator;
            _currentValidator.ForceValidationCheck();
        }

        private void RefreshConfirmButton(bool value, string message)
        {
            if (value)
            {
                _confirmButton.Enable();
                _confirmButton.tooltip = string.Empty;
            }
            else
            {
                _confirmButton.Disable();
                _confirmButton.tooltip = message;
            }
        }

        public void Set(Schedule schedule, EventContent content)
        {
            _descriptionComponent.SetValueWithoutNotify(schedule.description);
            _eventNameComponent.SetValueWithoutNotify(content.name);

            var neverExpires = !schedule.activeTo.HasValue;
            if (!neverExpires && schedule.TryGetActiveTo(out var activeToDate))
            {
                _activeToDateComponent.Set(activeToDate);
                _activeToHourComponent.Set(activeToDate);
            }

            _neverExpiresComponent.Value = neverExpires;

            if (schedule.TryGetActiveFrom(out var activeFromDate))
            {
                _startTimeComponent.Set(activeFromDate);
            }

            var explicitDates = schedule.definitions.Any(definition => definition.dayOfMonth.Any(day => day != "*"));
            var hasDaysOfWeek = schedule.definitions.Any(definition => definition.dayOfWeek.Any(day => day != "*"));
            if (explicitDates)
            {
                _dropdownComponent.Set("Actual dates");
                _calendarComponent.Calendar.SetInitialValues(schedule.definitions);
            }
            else if (hasDaysOfWeek)
            {
                _dropdownComponent.Set("Days of week");
                _daysPickerComponent.SetSelectedDays(schedule.definitions[0].dayOfWeek);
            }
        }

        public void ApplyDataTransforms(EventContent data)
        {
            data.name = _eventNameComponent.Value;
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

        private void ConfirmClicked()
        {
            Schedule newSchedule = new Schedule();

            _scheduleParser.PrepareGeneralData(newSchedule, _descriptionComponent.Value,
                _startTimeComponent.SelectedHour, _neverExpiresComponent.Value,
                $"{_activeToDateComponent.SelectedDate}{_activeToHourComponent.SelectedHour}");

            switch (_currentMode)
            {
                case Mode.Daily:
                    _scheduleParser.PrepareDailyModeData(newSchedule, "0", "0", "0");
                    break;
                case Mode.Days:
                    _scheduleParser.PrepareDaysModeData(newSchedule, _startTimeComponent.Hour,
                        _startTimeComponent.Minute, _startTimeComponent.Second,
                        _daysPickerComponent.DaysPicker.GetSelectedDays());
                    break;
                case Mode.Dates:
                    _scheduleParser.PrepareDateModeData(newSchedule, _calendarComponent.SelectedDays,
                        _startTimeComponent.Hour, _startTimeComponent.Minute, _startTimeComponent.Second);
                    break;
            }

            OnScheduleUpdated?.Invoke(newSchedule);
        }

        private void CancelClicked()
        {
            OnCancelled?.Invoke();
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
                ve.EnableInClassList("--positionHidden", !ve.visible);
            }
        }

        private void OnModeChanged(string option)
        {
            if (_modes.TryGetValue(option, out Mode newMode))
            {
                _currentMode = newMode;
            }

            switch (_currentMode)
            {
                case Mode.Daily:
                    _currentValidator = _dailyModeValidator;
                    break;
                case Mode.Days:
                    _currentValidator = _daysModeValidator;
                    break;
                case Mode.Dates:
                    _currentValidator = _datesModeValidator;
                    break;
            }

            _currentValidator?.ForceValidationCheck();

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