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
    public class LabeledNumberPicker : BeamableVisualElement
    {
        private LabeledTextField _labeledTextFieldComponent;
        private Button _button;

        public new class UxmlFactory : UxmlFactory<LabeledNumberPicker, UxmlTraits>
        {
        }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
                {name = "label", defaultValue = "Label"};

            readonly UxmlStringAttributeDescription _value = new UxmlStringAttributeDescription
                {name = "value"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is LabeledNumberPicker component)
                {
                    component.Label = _label.GetValueFromBag(bag, cc);
                    component.Value = _value.GetValueFromBag(bag, cc);
                }
            }
        }

        public string Value { get; set; }
        public string Label { get; set; }

        public LabeledNumberPicker() : base($"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledNumberPicker)}/{nameof(LabeledNumberPicker)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _labeledTextFieldComponent = Root.Q<LabeledTextField>("labelAndValue");
            _labeledTextFieldComponent.Label = Label;
            _labeledTextFieldComponent.Value = Value;
            _labeledTextFieldComponent.Refresh();

            _button = Root.Q<Button>("button");
        }
    }
}