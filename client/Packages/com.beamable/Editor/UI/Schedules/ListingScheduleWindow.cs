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
        private LabeledCalendarVisualElement _calendarComponent;
        private GenericButtonVisualElement _cancelButton;

        private VisualElement _daysGroup;
        private VisualElement _datesGroup;
        private PrimaryButtonVisualElement _confirmButton;

        // TODO: create some generic composite rules for cases like this one and then remove below fields
        private bool _isPeriodValid;
        private string _invalidPeriodMessage;

        private readonly List<ScheduleWindowModel> _models = new List<ScheduleWindowModel>();
        private ScheduleWindowModel _currentModel;

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
        }

        public override void Refresh()
        {
            base.Refresh();

            _eventNameComponent = Root.Q<LabeledTextField>("eventName");
            _eventNameComponent.Refresh();

            _descriptionComponent = Root.Q<LabeledTextField>("description");
            _descriptionComponent.Refresh();

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

            _cancelButton = Root.Q<GenericButtonVisualElement>("cancelBtn");
            _cancelButton.OnClick += CancelClicked;

            // Groups
            _daysGroup = Root.Q<VisualElement>("daysGroup");
            _datesGroup = Root.Q<VisualElement>("datesGroup");

            SetupModels();

            _dropdownComponent = Root.Q<LabeledDropdownVisualElement>("dropdown");
            _dropdownComponent.Setup(PrepareOptions(), OnModeChanged);
            _dropdownComponent.Refresh();

            RefreshGroups();
            OnExpirationChanged(_neverExpiresComponent.Value);
            OnAllDayChanged(_allDayComponent.Value);

            // TODO: create some generic composite rules for cases like this one and then remove below lines
            _periodFromHourComponent.OnValueChanged = PerformPeriodValidation;
            _periodToHourComponent.OnValueChanged = PerformPeriodValidation;

            EditorApplication.delayCall += () => { _currentModel.ForceValidationCheck(); };
        }

        private void SetupModels()
        {
            ListingDailyScheduleModel dailyModel = new ListingDailyScheduleModel(_descriptionComponent,
                _neverExpiresComponent, _activeToDateComponent, _activeToHourComponent, _allDayComponent,
                _periodFromHourComponent, _periodToHourComponent, RefreshConfirmButton);

            ListingDaysScheduleModel daysModel = new ListingDaysScheduleModel(_descriptionComponent,
                _daysPickerComponent, _neverExpiresComponent, _activeToDateComponent, _activeToHourComponent,
                _allDayComponent, _periodFromHourComponent, _periodToHourComponent, RefreshConfirmButton);

            ListingDatesScheduleModel datesModel = new ListingDatesScheduleModel(_descriptionComponent,
                _calendarComponent, _neverExpiresComponent, _activeToDateComponent, _activeToHourComponent,
                _allDayComponent, _periodFromHourComponent, _periodToHourComponent, RefreshConfirmButton);

            _models.Clear();
            _models.Add(dailyModel);
            _models.Add(daysModel);
            _models.Add(datesModel);
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
                _isPeriodValid = _currentModel.Mode != ScheduleWindowModel.WindowMode.Daily;
                _invalidPeriodMessage = _currentModel.Mode == ScheduleWindowModel.WindowMode.Daily
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

            _currentModel?.ForceValidationCheck();
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
            if (_cancelButton != null) _cancelButton.OnClick -= CancelClicked;
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
                _dropdownComponent.Set(2);
                _calendarComponent.Calendar.SetInitialValues(schedule.definitions);
            }
            else if (hasDaysOfWeek)
            {
                _dropdownComponent.Set(1);
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
            OnScheduleUpdated?.Invoke(_currentModel.GetSchedule());
        }

        private void CancelClicked()
        {
            OnCancelled?.Invoke();
        }

        private void RefreshGroups()
        {
            RefreshSingleGroup(_daysGroup, ScheduleWindowModel.WindowMode.Days);
            RefreshSingleGroup(_datesGroup, ScheduleWindowModel.WindowMode.Dates);
        }

        private void RefreshSingleGroup(VisualElement ve, ScheduleWindowModel.WindowMode mode)
        {
            if (ve == null) return;
            ve.visible = _currentModel.Mode == mode;
            ve.EnableInClassList("--positionHidden", !ve.visible);
        }

        private void OnModeChanged(int option)
        {
            _currentModel = _models[option];

			if (_currentModel?.Mode == ScheduleWindowModel.WindowMode.Dates)
				_calendarComponent.Calendar.SetDefaultValues();

            _currentModel.ForceValidationCheck();
            RefreshGroups();
            PerformPeriodValidation();
		}

        private List<string> PrepareOptions()
        {
            return _models.Select(model => model.Mode.ToString()).ToList();
        }
    }
}
