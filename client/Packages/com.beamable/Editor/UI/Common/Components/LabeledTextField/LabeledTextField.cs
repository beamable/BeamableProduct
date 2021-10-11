using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
using Editor.UI.Validation;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
    public class LabeledTextField : ValidableVisualElement<string>
    {
        public new class UxmlFactory : UxmlFactory<LabeledTextField, UxmlTraits>
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
                if (ve is LabeledTextField component)
                {
                    component.Label = _label.GetValueFromBag(bag, cc);
                    component.Value = _value.GetValueFromBag(bag, cc);
                }
            }
        }

        private Label _label;
        private TextField _textField;

        public string Label { get; set; }
        public string Value { get; set; }

        public LabeledTextField() : base($"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledTextField)}/{nameof(LabeledTextField)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _label = Root.Q<Label>("label");
            _label.text = Label;

            _textField = Root.Q<TextField>("textField");
            _textField.value = Value;
            
            

            _textField.RegisterValueChangedCallback(ValueChanged);
        }

        public void SetValueWithoutNotify(string value)
        {
            // TODO: we shouldn't need to set it up this way, Ideally we could just use the Property setter
            Value = value;
            _textField.SetValueWithoutNotify(value);
        }

        protected override void OnDestroy()
        {
            _textField.UnregisterValueChangedCallback(ValueChanged);
        }

        private void ValueChanged(ChangeEvent<string> evt)
        {
            Value = evt.newValue;
            InvokeValidationCheck(Value);
        }
    }
}