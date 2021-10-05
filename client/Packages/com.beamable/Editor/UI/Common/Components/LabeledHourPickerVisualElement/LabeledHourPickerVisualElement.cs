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
    public class LabeledHourPickerVisualElement : BeamableVisualElement
    {
        private Label _label;
        private HourPickerVisualElement _hourPicker;

        public new class UxmlFactory : UxmlFactory<LabeledHourPickerVisualElement, UxmlTraits>
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
                if (ve is LabeledHourPickerVisualElement component)
                {
                    component.Label = _label.GetValueFromBag(bag, cc);
                }
            }
        }

        public string Label { get; set; }
        public string SelectedHour => _hourPicker.GetHour();

        public LabeledHourPickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledHourPickerVisualElement)}/{nameof(LabeledHourPickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _label = Root.Q<Label>("label");
            _label.text = Label;

            _hourPicker = Root.Q<HourPickerVisualElement>("hourPicker");
            _hourPicker.Refresh();
        }
    }
}