using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
    public class HourPickerVisualElement : BeamableVisualElement
    {
        private LabeledNumberPicker _hourPicker;
        private LabeledNumberPicker _minutePicker;
        private LabeledNumberPicker _secondPicker;

        public new class UxmlFactory : UxmlFactory<HourPickerVisualElement, UxmlTraits>
        {
        }

        public HourPickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(HourPickerVisualElement)}/{nameof(HourPickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _hourPicker = Root.Q<LabeledNumberPicker>("hourPicker");
            _hourPicker.Setup(GenerateHours());
            _hourPicker.Refresh();
            
            _minutePicker = Root.Q<LabeledNumberPicker>("minutePicker");
            _minutePicker.Setup(GenerateMinutesAndSeconds());
            _minutePicker.Refresh();
            
            _secondPicker = Root.Q<LabeledNumberPicker>("secondPicker");
            _secondPicker.Setup(GenerateMinutesAndSeconds());
            _secondPicker.Refresh();
        }

        private List<string> GenerateHours()
        {
            List<string> options = new List<string>();

            for (int i = 0; i < 24; i++)
            {
                string hour = (i + 1).ToString("00");
                options.Add(hour);
            }

            return options;
        }

        private List<string> GenerateMinutesAndSeconds()
        {
            List<string> options = new List<string>();

            for (int i = 0; i < 4; i++)
            {
                string hour = (i * 15).ToString("00");
                options.Add(hour);
            }

            return options;
        }
    }
}