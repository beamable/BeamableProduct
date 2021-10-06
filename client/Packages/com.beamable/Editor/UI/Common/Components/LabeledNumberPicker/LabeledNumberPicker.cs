using System.Collections.Generic;
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
    public class LabeledNumberPicker : BeamableVisualElement
    {
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
        
        private LabeledTextField _labeledTextFieldComponent;
        private Button _button;
        private List<string> _options;

        public string Value { get; private set; }
        private string Label { get; set; }

        public LabeledNumberPicker() : base($"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledNumberPicker)}/{nameof(LabeledNumberPicker)}")
        {
            _options = new List<string>();
        }

        public override void Refresh()
        {
            base.Refresh();

            _labeledTextFieldComponent = Root.Q<LabeledTextField>("labelAndValue");
            _labeledTextFieldComponent.Label = Label;
            _labeledTextFieldComponent.Value = Value;
            _labeledTextFieldComponent.Refresh();

            _button = Root.Q<Button>("button");

            ConfigureOptions();
        }

        public void Setup(List<string> options, bool active = true)
        {
            SetEnabled(active);
            _options = options;
        }

        private void ConfigureOptions()
        {
            ContextualMenuManipulator manipulator = new ContextualMenuManipulator(BuildOptions);
            manipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            _button.clickable.activators.Clear();
            _button.AddManipulator(manipulator);

            if (_options != null && _options.Count > 0)
            {
                SetOption(_options[0]);
            }
        }

        private void BuildOptions(ContextualMenuPopulateEvent evt)
        {
            foreach (string option in _options)
            {
                evt.menu.BeamableAppendAction(option, (pos) =>
                {
                    SetOption(option);
                });
            }
        }

        private void SetOption(string value)
        {
            Value = value;
            _labeledTextFieldComponent.Value = Value;
            _labeledTextFieldComponent.Refresh();
        }
    }
}