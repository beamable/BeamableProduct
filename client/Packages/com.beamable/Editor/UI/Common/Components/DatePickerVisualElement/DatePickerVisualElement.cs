using System;
using System.Collections.Generic;
using System.Text;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Validation;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
    public class DatePickerVisualElement : ValidableVisualElement<string>
    {
        public new class UxmlFactory : UxmlFactory<DatePickerVisualElement, UxmlTraits>
        {
        }

        private Action _onDateChanged;

        public LabeledNumberPicker YearPicker { get; private set; }
        public LabeledNumberPicker MonthPicker { get; private set; }
        public LabeledNumberPicker DayPicker { get; private set; }

        public DatePickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(DatePickerVisualElement)}/{nameof(DatePickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            YearPicker = Root.Q<LabeledNumberPicker>("yearPicker");
            YearPicker.Setup(OnDateChanged, GenerateYears());
            YearPicker.Refresh();

            MonthPicker = Root.Q<LabeledNumberPicker>("monthPicker");
            MonthPicker.Setup(OnDateChanged, GenerateMonths());
            MonthPicker.Refresh();

            DayPicker = Root.Q<LabeledNumberPicker>("dayPicker");
            DayPicker.Setup(OnDateChanged, GenerateDays());
            DayPicker.Refresh();
        }

        public string GetIsoDate()
        {
            return $"{GetSimpleDate()}T";
        }

        private void OnDateChanged()
        {
            InvokeValidationCheck(GetSimpleDate());
            _onDateChanged?.Invoke();
        }

        private string GetSimpleDate()
        {
            if (YearPicker == null || MonthPicker == null || DayPicker == null)
            {
                return string.Empty;
            }
            
            StringBuilder builder = new StringBuilder();
            builder.Append(
                $"{int.Parse(YearPicker.Value):0000}-{int.Parse(MonthPicker.Value):00}-{int.Parse(DayPicker.Value):00}");
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
                string option = (i + 1).ToString("00");
                options.Add(option);
            }

            return options;
        }

        private List<string> GenerateDays()
        {
            List<string> options = new List<string>();

            for (int i = 0; i < 31; i++)
            {
                string option = (i + 1).ToString("00");
                options.Add(option);
            }

            return options;
        }

        public void Set(DateTime date)
        {
            YearPicker.Set(date.Year.ToString());
            MonthPicker.Set(date.Month.ToString());
            DayPicker.Set((date.Day).ToString());
        }

        public void Setup(Action onDateChanged)
        {
            _onDateChanged = onDateChanged;
        }
    }
}