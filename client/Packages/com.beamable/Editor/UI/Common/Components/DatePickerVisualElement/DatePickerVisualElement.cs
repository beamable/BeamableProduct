using System;
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
    public class DatePickerVisualElement : BeamableVisualElement
    {
        private LabeledNumberPicker _yearPicker;
        private LabeledNumberPicker _monthPicker;
        private LabeledNumberPicker _dayPicker;

        public new class UxmlFactory : UxmlFactory<DatePickerVisualElement, UxmlTraits>
        {
        }

        public DatePickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(DatePickerVisualElement)}/{nameof(DatePickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _yearPicker = Root.Q<LabeledNumberPicker>("yearPicker");
            _yearPicker.Setup(GenerateYears());
            _yearPicker.Refresh();
            
            _monthPicker = Root.Q<LabeledNumberPicker>("monthPicker");
            _monthPicker.Setup(GenerateMonths());
            _monthPicker.Refresh();
            
            _dayPicker = Root.Q<LabeledNumberPicker>("dayPicker");
            _dayPicker.Setup(GenerateDays());
            _dayPicker.Refresh();
        }
        
        public string GetHour()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"{_yearPicker.Value}:{_monthPicker.Value}:{_dayPicker.Value}");
            return builder.ToString();
        }

        private List<string> GenerateYears()
        {
            int yearsAdvance = 3;
            
            List<string> options = new List<string>();

            DateTime now = DateTime.Now;

            for (int i = 0; i < yearsAdvance; i++)
            {
                string option = (now.Year + i).ToString("0000");
                options.Add(option);
            }

            return options;
        }

        private List<string> GenerateMonths()
        {
            List<string> options = new List<string>();
            
            for (int i = 0; i < 12; i++)
            {
                string option = (i+1).ToString("00");
                options.Add(option);
            }

            return options;
        }
        
        private List<string> GenerateDays()
        {
            List<string> options = new List<string>();
            
            for (int i = 0; i < 31; i++)
            {
                string option = (i+1).ToString("00");
                options.Add(option);
            }

            return options;
        }
    }
}