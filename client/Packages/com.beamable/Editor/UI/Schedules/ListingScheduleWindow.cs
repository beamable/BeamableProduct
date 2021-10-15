using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Content;
using Beamable.Common.Shop;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Validation;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Schedules
{
    public class ListingScheduleWindow : BeamableVisualElement, IScheduleWindow<ListingContent>
    {
        public event Action<Schedule> OnScheduleUpdated;
        public event Action OnCancelled;

        private enum Mode
        {
            Daily,
            Days,
            Dates
        }

        private LabeledTextField _eventNameComponent;
        private LabeledTextField _descriptionComponent;
        private LabeledDatePickerVisualElement _activeToDateComponent;
        private LabeledHourPickerVisualElement _activeToHourComponent;
        private LabeledDropdownVisualElement _dropdownComponent;
        private LabeledCheckboxVisualElement _neverExpiresComponent;
        private LabeledDaysPickerVisualElement _daysPickerComponent;
        private LabeledCheckboxVisualElement _allDayComponent;
        private LabeledHourPickerVisualElement _periodFromHourComponent;
        private LabeledHourPickerVisualElement _periodToHourComponent;

        private VisualElement _daysGroup;
        private VisualElement _datesGroup;
        private PrimaryButtonVisualElement _confirmButton;

        private readonly Dictionary<string, Mode> _modes;
        private Mode _currentMode;
        private Button _cancelButton;
        private ComponentsValidator _dailyModeValidator;
        private ComponentsValidator _daysModeValidator;
        private ComponentsValidator _datesModeValidator;
        private ComponentsValidator _currentValidator;
        private LabeledCalendarVisualElement _calendarComponent;
        private readonly ScheduleParser _scheduleParser;

        // TODO: create some generic composite rules for cases like this one and then remove below fields
        private bool _isPeriodValid;
        private string _invalidPeriodMessage;

        public ListingScheduleWindow() : base(
            $"{BeamableComponentsConstants.SCHEDULES_PATH}/{nameof(ListingScheduleWindow)}")
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

            _dropdownComponent = Root.Q<LabeledDropdownVisualElement>("dropdown");
            _dropdownComponent.Setup(PrepareOptions(), OnModeChanged);
            _dropdownComponent.Refresh();

            // Periods
            _allDayComponent = Root.Q<LabeledCheckboxVisualElement>("allDay");
            _allDayComponent.OnValueChanged += OnAllDayChanged;
            _allDayComponent.Refresh();
            _allDayComponent.DisableIcon();

            _periodFromHourComponent = Root.Q<LabeledHourPickerVisualElement>("periodFromHour");
            _periodFromHourComponent.Refresh();

            _periodToHourComponent = Root.Q<LabeledHourPickerVisualElement>("periodToHour");
            _periodToHourComponent.Refresh();

            // Active to
            _neverExpiresComponent = Root.Q<LabeledCheckboxVisualElement>("expiresNever");
            _neverExpiresComponent.OnValueChanged += OnExpirationChanged;
            _neverExpiresComponent.Refresh();
            _neverExpiresComponent.DisableIcon();

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
            OnAllDayChanged(_allDayComponent.Value);

            _dailyModeValidator = new ComponentsValidator(RefreshConfirmButton);

            _daysModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _daysModeValidator.RegisterRule(new AtLeastOneOptionSelectedRule(_daysPickerComponent.Label),
                _daysPickerComponent);
            _daysModeValidator.RegisterRule(new IsProperDate(_activeToDateComponent.Label), _activeToDateComponent.DatePicker);

            _datesModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _datesModeValidator.RegisterRule(new AtLeastOneOptionSelectedRule(_calendarComponent.Label),
                _calendarComponent);

            // TODO: create some generic composite rules for cases like this one and then remove below lines
            _periodFromHourComponent.OnValueChanged = PerformPeriodValidation;
            _periodToHourComponent.OnValueChanged = PerformPeriodValidation;
            _allDayComponent.OnValueChangedNotifier = PerformPeriodValidation;

            _currentValidator = _dailyModeValidator;
            _currentValidator.ForceValidationCheck();
        }

        // TODO: create some generic composite rules for cases like this one and then remove below lines
        private void PerformPeriodValidation()
        {
            if (_allDayComponent.Value)
            {
                _isPeriodValid = true;
                _invalidPeriodMessage = string.Empty;
            }
            else
            {
                HoursValidationRule rule =
                    new HoursValidationRule(_periodFromHourComponent.Label, _periodToHourComponent.Label);
                rule.Validate(_periodFromHourComponent.SelectedHour, _periodToHourComponent.SelectedHour);
                _isPeriodValid = rule.Satisfied;
                _invalidPeriodMessage = rule.ErrorMessage;
            }

            _currentValidator.ForceValidationCheck();
        }

        private void RefreshConfirmButton(bool value, string message)
        {
            bool validated = value && _isPeriodValid;

            if (validated)
            {
                _confirmButton.Enable();
                _confirmButton.tooltip = string.Empty;
            }
            else
            {
                _confirmButton.Disable();

                string fullMessage = message;

                if (!_isPeriodValid)
                {
                    fullMessage += $"\n{_invalidPeriodMessage}";
                }

                _confirmButton.tooltip = fullMessage;
            }
        }

        protected override void OnDestroy()
        {
            if (_neverExpiresComponent != null) _neverExpiresComponent.OnValueChanged -= OnExpirationChanged;
            if (_allDayComponent != null) _allDayComponent.OnValueChanged -= OnAllDayChanged;
            if (_confirmButton != null) _confirmButton.Button.clickable.clicked -= ConfirmClicked;
            if (_cancelButton != null) _cancelButton.clickable.clicked -= CancelClicked;
        }

        public void Set(Schedule schedule, ListingContent content)
        {
            _descriptionComponent.Value = schedule.description;

            _eventNameComponent.SetEnabled(false);
            _eventNameComponent.Value = content.name;

            var neverExpires = !schedule.activeTo.HasValue;
            if (!neverExpires && schedule.TryGetActiveTo(out var activeToDate))
            {
                _activeToDateComponent.Set(activeToDate);
                _activeToHourComponent.Set(activeToDate);
            }

            _neverExpiresComponent.Value = neverExpires;

            var isPeriod = schedule.definitions.Any(definition =>
                definition.hour.Any(x => x.Contains("-"))
                || definition.minute.Any(x => x.Contains("-"))
                || definition.second.Any(x => x.Contains("-"))
            );
            _allDayComponent.Value = !isPeriod;

            if (isPeriod)
            {
                // TODO: What happens where there is more than one period?
                _periodFromHourComponent.SetPeriod(schedule.definitions[0], 0);
                _periodToHourComponent.SetPeriod(schedule.definitions[0], 1);
            }

            if (schedule.TryGetActiveFrom(out var activeFromDate))
            {
                // _startTimeComponent.Set(activeFromDate);
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

        public void ApplyDataTransforms(ListingContent data)
        {
            // nothing to do.
        }

        private void OnAllDayChanged(bool value)
        {
            _periodFromHourComponent.SetGroupEnabled(!value);
            _periodToHourComponent.SetGroupEnabled(!value);
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
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), _neverExpiresComponent.Value,
                $"{_activeToDateComponent.SelectedDate}{_activeToHourComponent.SelectedHour}");

            switch (_currentMode)
            {
                case Mode.Daily:
                    _scheduleParser.PrepareDailyModeData(newSchedule, PrepareSecondRange(), PrepareMinuteRange(),
                        PreparePeriodRange());
                    break;
                case Mode.Days:
                    _scheduleParser.PrepareDaysModeData(newSchedule, PrepareSecondRange(), PrepareMinuteRange(),
                        PreparePeriodRange(), _daysPickerComponent.DaysPicker.GetSelectedDays());
                    break;
                case Mode.Dates:
                    _scheduleParser.PrepareDateModeData(newSchedule, _calendarComponent.SelectedDays,
                        PreparePeriodRange(), "*", "*");
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

        private string PreparePeriodRange()
        {
            string hourString = _allDayComponent.Value
                ? "*"
                : $"{_periodFromHourComponent.Hour}-{_periodToHourComponent.Hour}";
            return hourString;
        }

        private string PrepareMinuteRange()
        {
            string hourString = _allDayComponent.Value
                ? "*"
                : $"{_periodFromHourComponent.Minute}-{_periodToHourComponent.Minute}";
            return hourString;
        }

        private string PrepareSecondRange()
        {
            string hourString = _allDayComponent.Value
                ? "*"
                : $"{_periodFromHourComponent.Second}-{_periodToHourComponent.Second}";
            return hourString;
        }
    }
}