using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
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
    public class ScheduleWindow : BeamableVisualElement
    {
        private LabeledTextField _eventNameComponent;
        private LabeledTextField _descriptionComponent;
        private LabeledHourPickerVisualElement _startTime;
        private LabeledDatePickerVisualElement _activeToDate;
        private LabeledHourPickerVisualElement _activeToHour;

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
        }
    }
}