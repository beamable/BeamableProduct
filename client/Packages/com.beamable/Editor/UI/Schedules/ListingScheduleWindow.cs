using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Content;
using System.Text.RegularExpressions;
using Beamable.Common.Shop;
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
    public class ListingScheduleWindow : BeamableVisualElement, IScheduleWindow<ListingContent>
    {
        public Action OnCancel;
        public Action<string> OnConfirm;
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
        private BeamableCheckboxVisualElement _neverExpiresComponent;
        private LabeledDaysPickerVisualElement _daysDaysPickerComponent;
        private BeamableCheckboxVisualElement _allDayComponent;
        private LabeledHourPickerVisualElement _periodFromHourComponent;
        private LabeledHourPickerVisualElement _periodToHourComponent;
        private LabeledTextField _datesField;

        private VisualElement _daysGroup;
        private VisualElement _datesGroup;
        private PrimaryButtonVisualElement _confirmButton;

        private readonly Dictionary<string, Mode> _modes;
        private Mode _currentMode;
        private Button _cancelButton;
        
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
            _allDayComponent = Root.Q<BeamableCheckboxVisualElement>("allDay");
            _allDayComponent.OnValueChanged += OnAllDayChanged;
            _allDayComponent.Refresh();

            _periodFromHourComponent = Root.Q<LabeledHourPickerVisualElement>("periodFromHour");
            _periodFromHourComponent.Refresh();

            _periodToHourComponent = Root.Q<LabeledHourPickerVisualElement>("periodToHour");
            _periodToHourComponent.Refresh();

            // Active to
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
            _descriptionComponent.SetValueWithoutNotify(schedule.description);

            _eventNameComponent.SetEnabled(false);
            _eventNameComponent.SetValueWithoutNotify(content.name);

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
                var definition = schedule.definitions[0];

                _daysDaysPickerComponent.SetSelectedDays(schedule.definitions[0].dayOfWeek);

                // definition.hour[0].Split()

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
            newSchedule.activeFrom = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            newSchedule.activeTo.HasValue = !_neverExpiresComponent.Value;
            newSchedule.activeTo.Value = $"{_activeToDateComponent.SelectedDate}{_activeToHourComponent.SelectedHour}";
        }

        private void PrepareDailyModeData(Schedule newSchedule)
        {
            ScheduleDefinition definition =
                new ScheduleDefinition(PrepareSecondRange(), PrepareMinuteRange(), PreparePeriodRange(), new List<string> {"*"}, "*", "*", new List<string> {"*"});
            newSchedule.AddDefinition(definition);
        }

        private void PrepareDaysModeData(Schedule newSchedule)
        {
            ScheduleDefinition definition = new ScheduleDefinition(PrepareSecondRange(), PrepareMinuteRange(), PreparePeriodRange(), new List<string> {"*"}, "*", "*",
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

                ScheduleDefinition definition = new ScheduleDefinition("*",
                    "*", PreparePeriodRange(), daysInCurrentMonthAndYear, month, year,
                    new List<string> {"*"});

                newSchedule.AddDefinition(definition);
            }
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