using System;
using System.Collections.Generic;
using System.Linq;
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
    public class LabeledDatePickerVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<LabeledDatePickerVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
                {name = "label", defaultValue = "Label"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is LabeledDatePickerVisualElement component)
                {
                    component.Label = _label.GetValueFromBag(bag, cc);
                }
            }
        }
        
        public Action OnValueChanged;

        private Label _label;
        
        public DatePickerVisualElement DatePicker { get; private set; }
        public string Label { get; private set; }
        public string SelectedDate => DatePicker.GetIsoDate();

        public LabeledDatePickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledDatePickerVisualElement)}/{nameof(LabeledDatePickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _label = Root.Q<Label>("label");
            _label.text = Label;

            DatePicker = Root.Q<DatePickerVisualElement>("datePicker");
            DatePicker.Setup(OnDateChanged);
            DatePicker.Refresh();
        }
        
        public void Set(DateTime date) => DatePicker.Set(date);

        private void OnDateChanged()
        {
            if (DatePicker == null || DatePicker.YearPicker == null ||
                DatePicker.MonthPicker == null || DatePicker.DayPicker == null)
            {
                return;
            }

            if (DatePicker.YearPicker.Value.Length > 4)
            {
                DatePicker.YearPicker.Value = DatePicker.YearPicker.Value.Substring(0, 4);
            }
            
            int.TryParse(DatePicker.MonthPicker.Value, out int resultMonth);
            if (!Enumerable.Range(1, 12).Contains(resultMonth))
            {
                resultMonth = Mathf.Clamp(resultMonth, 1, 12);
                DatePicker.MonthPicker.Value = resultMonth.ToString();
            }
            
            int.TryParse(DatePicker.DayPicker.Value, out int resultDay);
            if (!Enumerable.Range(1, 31).Contains(resultDay))
            {
                resultDay = Mathf.Clamp(resultDay, 1, 31);
                DatePicker.DayPicker.Value = resultDay.ToString();
            }

            OnValueChanged?.Invoke(); 
        }
    }
}