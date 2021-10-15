using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Validation;
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
    public class LabeledTextField : ValidableVisualElement<string>
    {
        public enum Mode
        {
            Default,
            DigitsOnly
        }

        public new class UxmlFactory : UxmlFactory<LabeledTextField, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
                {name = "label", defaultValue = "Label"};

            readonly UxmlStringAttributeDescription _value = new UxmlStringAttributeDescription
                {name = "value"};

            readonly UxmlStringAttributeDescription _mode = new UxmlStringAttributeDescription
                {name = "mode", defaultValue = "default"};

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

                    bool parse = Enum.TryParse(_mode.GetValueFromBag(bag, cc), true, out Mode parsedMode);
                    component.WorkingMode = parse ? parsedMode : Mode.Default;
                }
            }
        }

        public Action OnValueChanged;

        private Label _label;
        private TextField _textField;
        private string _value;

        public Mode WorkingMode { get; set; }
        public string Label { get; set; }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                _textField?.SetValueWithoutNotify(_value);
                OnValueChanged?.Invoke();
            }
        }

        public LabeledTextField() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledTextField)}/{nameof(LabeledTextField)}")
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

        protected override void OnDestroy()
        {
            _textField.UnregisterValueChangedCallback(ValueChanged);
        }

        private void ValueChanged(ChangeEvent<string> evt)
        {
            Value = ValidateValue(evt.newValue);
            InvokeValidationCheck(Value);
        }

        private string ValidateValue(string value)
        {
            switch (WorkingMode)
            {
                case Mode.Default:
                    return value;
                case Mode.DigitsOnly:
                    if (string.IsNullOrEmpty(value))
                    {
                        value = "0";
                    }
                    return new string(value.Where(Char.IsDigit).ToArray());
            }

            return value;
        }
    }
}