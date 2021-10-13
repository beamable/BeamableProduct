using System;
using System.Collections.Generic;
using System.Text;
using Beamable.Common.Content;
using Beamable.Editor.UI.Buss;
using UnityEngine;
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
            builder.Append($"{int.Parse(_hourPicker.Value):00}:{int.Parse(_minutePicker.Value):00}:{int.Parse(_secondPicker.Value):00}Z");
            return builder.ToString();
        }

        private List<string> GenerateHours()
        {
            List<string> options = new List<string>();

            for (int i = 0; i < 24; i++)
            {
                string hour = (i).ToString("00");
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

        public void Set(DateTime date)
        {
            _hourPicker.Set(date.Hour.ToString());
            _minutePicker.Set(date.Minute.ToString());
            _secondPicker.Set(date.Second.ToString());
        }

        public void SetPeriod(ScheduleDefinition definition, int index)
        {
            _hourPicker.Set(definition.hour[0].Split('-')[index]);
            _minutePicker.Set(definition.minute[0].Split('-')[index]);
            _secondPicker.Set(definition.second[0].Split('-')[index]);
        }

        public void SetGroupEnabled(bool b)
        {
            _hourPicker.SetEnabled(b);
            _minutePicker.SetEnabled(b);
            _secondPicker.SetEnabled(b);
        }
    }
}