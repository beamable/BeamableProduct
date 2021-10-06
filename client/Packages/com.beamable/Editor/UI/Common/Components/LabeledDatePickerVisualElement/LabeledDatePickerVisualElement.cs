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

        private Label _label;
        private DatePickerVisualElement _datePicker;

        private string Label { get; set; }
        public string SelectedDate => _datePicker.GetDate();

        public LabeledDatePickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledDatePickerVisualElement)}/{nameof(LabeledDatePickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _label = Root.Q<Label>("label");
            _label.text = Label;

            _datePicker = Root.Q<DatePickerVisualElement>("datePicker");
            _datePicker.Refresh();
        }

        // public void SetEnabled(bool value)
        // {
        //     _label.SetEnabled(value);
        //     _datePicker.SetEnabled(value);
        // }
    }
}