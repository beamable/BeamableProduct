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
        public Action OnCancel;
        public event Action OnCancelled;
        public Action<string> OnConfirm;
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
        private LabeledTextField _datesField;
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
            _daysPickerComponent = Root.Q<LabeledDaysPickerVisualElement>("daysPicker");
            _daysPickerComponent.Refresh();

            // Date mode
            // TODO: add calendar component
            _datesField = Root.Q<LabeledTextField>("datesField");
            _datesField.Refresh();

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
            // TODO: remove both rules before release (showcase only)
            _dailyModeValidator.RegisterRuleForComponent(new IsNotEmptyRule(_eventNameComponent.Label), _eventNameComponent);
            _dailyModeValidator.RegisterRuleForComponent(new IsNotEmptyRule(_descriptionComponent.Label), _descriptionComponent);
            
            _daysModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _daysModeValidator.RegisterRuleForComponent(new AtLeastOneOptionSelectedRule(_daysPickerComponent.Label), _daysPickerComponent);
            
            _datesModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _datesModeValidator.RegisterRuleForComponent(new SchedulesDatesRule(_datesField.Label), _datesField);

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

            var explicitDates = schedule.definitions.Count > 1;
            var hasDaysOfWeek = schedule.definitions.Any(definition => definition.dayOfWeek.Any(day => day != "*"));
            if (explicitDates)
            {
                _dropdownComponent.Set("Actual dates");
                // TODO: What happens if the raw data has more than one recurrence?
                var dateStrings = schedule.definitions.Select(definition =>
                    $"{definition.year[0]:0000}-{definition.month[0]:00}-{definition.dayOfMonth[0]:00}").ToList();
                var dates = string.Join(";", dateStrings);

                _datesField.SetValueWithoutNotify(dates);

            } else if (hasDaysOfWeek)
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

            OnConfirm?.Invoke(replaced);
            OnScheduleUpdated?.Invoke(newSchedule);
        }

        private void CancelClicked()
        {
            OnCancel?.Invoke();
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

        #region Data parsing (to be moved to separate objects)

        private void PrepareGeneralData(Schedule newSchedule)
        {
            newSchedule.description = _descriptionComponent.Value;
            newSchedule.activeFrom = _startTimeComponent.SelectedHour;
            newSchedule.activeTo.HasValue = !_neverExpiresComponent.Value;
            newSchedule.activeTo.Value = $"{_activeToDateComponent.SelectedDate}{_activeToHourComponent.SelectedHour}";
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
                _daysPickerComponent.DaysPicker.GetSelectedDays());
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