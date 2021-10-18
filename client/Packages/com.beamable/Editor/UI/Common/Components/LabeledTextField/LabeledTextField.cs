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
        private enum Mode
        {
            Default,
            DigitsOnly,
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


        private Action _onValueChanged;
        private Label _labelComponent;
        private TextField _textFieldComponent;
        private string _value;
        private int _minValue;
        private int _maxValue;

        public string Value
        {
            get => ValidateOutputValue(_value);
            set
            {
                _value = value;
                _textFieldComponent?.SetValueWithoutNotify(_value);
                _onValueChanged?.Invoke();
            }
        }

        private Mode WorkingMode { get; set; }
        private string Label { get; set; }

        public LabeledTextField() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledTextField)}/{nameof(LabeledTextField)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _labelComponent = Root.Q<Label>("label");
            _labelComponent.text = Label;

            _textFieldComponent = Root.Q<TextField>("textField");
            _textFieldComponent.value = Value;
            _textFieldComponent.RegisterValueChangedCallback(ValueChanged);
        }

        public void Setup(string label, string value, Action onValueChanged, int minValue, int maxValue)
        {
            Label = label;
            Value = value;
            _onValueChanged = onValueChanged;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        protected override void OnDestroy()
        {
            _textFieldComponent.UnregisterValueChangedCallback(ValueChanged);
        }

        private void ValueChanged(ChangeEvent<string> evt)
        {
            Value = ValidateInputValue(evt.newValue);
            InvokeValidationCheck(Value);
        }

        private string ValidateInputValue(string value)
        {
            switch (WorkingMode)
            {
                case Mode.Default:
                    return value;
                case Mode.DigitsOnly:
                    return new string(value.Where(Char.IsDigit).ToArray());
            }

            return value;
        }

        private string ValidateOutputValue(string value)
        {
            string finalValue = value;
            
            switch (WorkingMode)
            {
                case Mode.DigitsOnly:
                    if (string.IsNullOrEmpty(value))
                    {
                        finalValue = _minValue.ToString();
                    }
                    else
                    {
                        if (int.TryParse(value, out int result))
                        {
                            result = Mathf.Clamp(result, _minValue, _maxValue);
                            finalValue = result.ToString();
                        }
                    }

                    break;
            }

            return finalValue;
        }
    }
}