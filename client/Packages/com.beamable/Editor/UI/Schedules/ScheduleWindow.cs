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
        private LabeledHourPickerVisualElement _startTime;
        private LabeledDatePickerVisualElement _activeToDate;
        private LabeledHourPickerVisualElement _activeToHour;
        private LabeledDropdownVisualElement _dropdown;

        private readonly Dictionary<string, Mode> _modes;
        private Mode _currentMode;
        private VisualElement _dailyGroup;
        private VisualElement _daysGroup;
        private VisualElement _datesGroup;


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

            _startTime = Root.Q<LabeledHourPickerVisualElement>("startTime");
            _startTime.Refresh();

            _activeToDate = Root.Q<LabeledDatePickerVisualElement>("activeToDate");
            _activeToDate.Refresh();

            _activeToHour = Root.Q<LabeledHourPickerVisualElement>("activeToHour");
            _activeToHour.Refresh();

            _dropdown = Root.Q<LabeledDropdownVisualElement>("dropdown");
            _dropdown.Setup(PrepareOptions(), OnModeChanged);
            _dropdown.Refresh();

            _dailyGroup = Root.Q<VisualElement>("dailyGroup");
            _daysGroup = Root.Q<VisualElement>("daysGroup");
            _datesGroup = Root.Q<VisualElement>("datesGroup");

            RefreshGroups();
        }

        private void RefreshGroups()
        {
            if (_dailyGroup != null) _dailyGroup.visible = _currentMode == Mode.DAILY;
            if (_daysGroup != null) _daysGroup.visible = _currentMode == Mode.DAYS;
            if (_datesGroup != null) _datesGroup.visible = _currentMode == Mode.DATES;
        }

        private void OnModeChanged(string option)
        {
            if (_modes.TryGetValue(option, out Mode newMode))
            {
                _currentMode = newMode;
                Debug.Log(_currentMode);
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