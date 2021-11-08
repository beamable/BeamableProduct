using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Content;
using Beamable.Common.Shop;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Validation;
using UnityEditor;
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
        
        #region Tests related properties and methods

        public LabeledDatePickerVisualElement ActiveToDateComponent => _activeToDateComponent;
        public LabeledHourPickerVisualElement ActiveToHourComponent => _activeToHourComponent;
        public LabeledHourPickerVisualElement PeriodFromHourComponent => _periodFromHourComponent;
        public LabeledHourPickerVisualElement PeriodToHourComponent => _periodToHourComponent;
        public LabeledCheckboxVisualElement NeverExpiresComponent => _neverExpiresComponent;
        public LabeledCheckboxVisualElement AllDayComponent => _allDayComponent;
        public LabeledDropdownVisualElement ModeComponent => _dropdownComponent;
        public LabeledDaysPickerVisualElement DaysComponent => _daysPickerComponent;
        public LabeledCalendarVisualElement CalendarComponent => _calendarComponent;
        public void InvokeTestConfirm() => ConfirmClicked();

        #endregion

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
            _daysModeValidator.RegisterRule(new AtLeastOneDaySelectedRule(_daysPickerComponent.Label),
                _daysPickerComponent);
            _daysModeValidator.RegisterRule(new NotAllDaysSelectedRule(_daysPickerComponent.Label),
                _daysPickerComponent);
            _daysModeValidator.RegisterRule(new IsProperDate(_activeToDateComponent.Label),
                _activeToDateComponent.DatePicker);

            _datesModeValidator = new ComponentsValidator(RefreshConfirmButton);
            _datesModeValidator.RegisterRule(new AtLeastOneDaySelectedRule(_calendarComponent.Label),
                _calendarComponent);

            // TODO: create some generic composite rules for cases like this one and then remove below lines
            _periodFromHourComponent.OnValueChanged = PerformPeriodValidation;
            _periodToHourComponent.OnValueChanged = PerformPeriodValidation;
            _currentValidator = _dailyModeValidator;

            EditorApplication.delayCall += () => { _currentValidator.ForceValidationCheck(); };
        }

        // TODO: create some generic composite rules for cases like this one and then remove below lines
        private void PerformPeriodValidation()
        {
            if (_allDayComponent == null)
            {
                return;
            }

            if (_allDayComponent.Value)
            {
                _isPeriodValid = _currentMode != Mode.Daily;
                _invalidPeriodMessage = _currentMode == Mode.Daily
                    ? "Daily mode can't have All day option selected"
                    : string.Empty;
            }
            else
            {
                HoursValidationRule rule =
                    new HoursValidationRule(_periodFromHourComponent.Label, _periodToHourComponent.Label);
                rule.Validate(_periodFromHourComponent.SelectedHour, _periodToHourComponent.SelectedHour);
                _isPeriodValid = rule.Satisfied;
                _invalidPeriodMessage = rule.ErrorMessage;
            }

            _currentValidator?.ForceValidationCheck();
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

            bool neverExpires = !schedule.activeTo.HasValue;
            if (!neverExpires && schedule.TryGetActiveTo(out var activeToDate))
            {
                _activeToDateComponent.Set(activeToDate);
                _activeToHourComponent.Set(activeToDate);
            }

            _neverExpiresComponent.Value = neverExpires;

            bool isPeriod = schedule.definitions.Any(def => def.hour[0].Contains("-")) ||
                            schedule.definitions.Any(def => def.minute[0].Contains("-")) ||
                            schedule.definitions.Any(def => def.second[0].Contains("-"));
            
            _allDayComponent.Value = !isPeriod;

            if (isPeriod)
            {
                int startHour = Convert.ToInt32(schedule.definitions[0].hour[0]);
                int endHour = Convert.ToInt32(schedule.definitions[schedule.definitions.Count - 1].hour[0]);

                string startMinutesRange = schedule.definitions[0].minute[0];
                string[] startSplitRange = startMinutesRange.Split('-');
                int startMinute = Convert.ToInt32(startSplitRange[0]);

                string endMinutesRange = schedule.definitions[schedule.definitions.Count - 1].minute[0];
                string[] endSplitRange = endMinutesRange.Split('-');
                int endMinute = Convert.ToInt32(endSplitRange[1]);

                _periodFromHourComponent.Set(new DateTime(2000, 1, 1, startHour, startMinute, 0));
                _periodToHourComponent.Set(new DateTime(2000, 1, 1, endHour, endMinute, 0));
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
            PerformPeriodValidation();
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

            int fromHour = 0;
            int toHour = 0;
            int fromMinute = 0;
            int toMinute = 0;
            
            if (!_allDayComponent.Value)
            {
                fromHour = int.Parse(_periodFromHourComponent.Hour);
                toHour = int.Parse(_periodToHourComponent.Hour);
                fromMinute = int.Parse(_periodFromHourComponent.Minute);
                toMinute = int.Parse(_periodToHourComponent.Minute);
            }

            switch (_currentMode)
            {
                case Mode.Daily:
                    if (!_allDayComponent.Value)
                    {
                        _scheduleParser.PrepareListingDailyModeData(newSchedule, fromHour, toHour, fromMinute,
                            toMinute);
                    }
                    break;
                case Mode.Days:
                    if (!_allDayComponent.Value)
                    {
                        _scheduleParser.PrepareListingDaysModeData(newSchedule, fromHour, toHour, fromMinute,
                            toMinute, _daysPickerComponent.DaysPicker.GetSelectedDays());
                    }
                    else
                    {
                        _scheduleParser.PrepareDaysModeData(newSchedule, "*", "*",
                            "*", _daysPickerComponent.DaysPicker.GetSelectedDays());
                    }
                    break;
                case Mode.Dates:
                    if (!_allDayComponent.Value)
                    {
                        _scheduleParser.PrepareListingDatesModeData(newSchedule, fromHour, toHour, fromMinute, toMinute, _calendarComponent.SelectedDays);
                    }
                    else
                    {
                        _scheduleParser.PrepareDateModeData(newSchedule, _calendarComponent.SelectedDays,
                            "*", "*", "*");
                    }
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
            PerformPeriodValidation();
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