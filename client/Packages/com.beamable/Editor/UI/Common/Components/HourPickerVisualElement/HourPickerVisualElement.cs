using System.Collections.Generic;
using System.Text;
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
        public new class UxmlFactory : UxmlFactory<HourPickerVisualElement, UxmlTraits>
        {
        }

        private LabeledNumberPicker _hourPicker;
        private LabeledNumberPicker _minutePicker;
        private LabeledNumberPicker _secondPicker;
        private bool _activeHour = true;
        private bool _activeMinute = true;
        private bool _activeSecond = true;

        public string Hour => _hourPicker.Value;
        public string Minute => _minutePicker.Value;
        public string Second => _secondPicker.Value;


        public HourPickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(HourPickerVisualElement)}/{nameof(HourPickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _hourPicker = Root.Q<LabeledNumberPicker>("hourPicker");
            _hourPicker.Setup(GenerateHours(), _activeHour);
            _hourPicker.Refresh();

            _minutePicker = Root.Q<LabeledNumberPicker>("minutePicker");
            _minutePicker.Setup(GenerateMinutesAndSeconds(), _activeMinute);
            _minutePicker.Refresh();

            _secondPicker = Root.Q<LabeledNumberPicker>("secondPicker");
            _secondPicker.Setup(GenerateMinutesAndSeconds(), _activeSecond);
            _secondPicker.Refresh();
        }

        public void Setup(bool activeHour = true, bool activeMinute = true, bool activeSecond = true)
        {
            _activeHour = activeHour;
            _activeMinute = activeMinute;
            _activeSecond = activeSecond;
        }

        public string GetFullHour()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"{_hourPicker.Value}:{_minutePicker.Value}:{_secondPicker.Value}.000Z");
            return builder.ToString();
        }

        private List<string> GenerateHours()
        {
            List<string> options = new List<string>();

            for (int i = 0; i < 24; i++)
            {
                string hour = (i).ToString();
                options.Add(hour);
            }

            return options;
        }

        private List<string> GenerateMinutesAndSeconds()
        {
            List<string> options = new List<string>();

            for (int i = 0; i < 4; i++)
            {
                string hour = (i * 15).ToString();
                options.Add(hour);
            }

            return options;
        }
    }
}