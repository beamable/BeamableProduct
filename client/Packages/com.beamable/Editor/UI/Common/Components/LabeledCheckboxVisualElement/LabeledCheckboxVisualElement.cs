using System;
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
    public class LabeledCheckboxVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<LabeledCheckboxVisualElement, UxmlTraits>
        {
        }

        public Action<bool> OnValueChanged;
        public bool Value
        {
            get => _checkbox.Value;
            set
            {
                SetWithoutNotify(value);
                OnValueChanged?.Invoke(value);
            }
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
            { name = "label", defaultValue = "Label" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is LabeledCheckboxVisualElement component)
                {
                    component.Label = _label.GetValueFromBag(bag, cc);
                }
            }
        }

        private Label _label;
        private BeamableCheckboxVisualElement _checkbox;

        private string Label { get; set; }


        public LabeledCheckboxVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledCheckboxVisualElement)}/{nameof(LabeledCheckboxVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _label = Root.Q<Label>("label");
            _label.text = Label;

            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _checkbox.OnValueChanged -= OnChanged;
            _checkbox.OnValueChanged += OnChanged;
            _checkbox.Refresh();
        }

        private void OnChanged(bool value)
        {
            OnValueChanged?.Invoke(value);
        }

        public void SetWithoutNotify(bool val) => _checkbox.SetWithoutNotify(val);
    }
}