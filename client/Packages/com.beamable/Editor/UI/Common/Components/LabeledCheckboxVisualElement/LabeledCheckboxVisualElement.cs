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
    public class LabeledVCheckboxVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<LabeledDatePickerVisualElement, UxmlTraits>
        {
        }

        public Action<bool> OnValueChanged;

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
            { name = "label", defaultValue = "Label" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        private Label _label;
        private BeamableCheckboxVisualElement _checkbox;

        private string Label { get; set; }

        public LabeledVCheckboxVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledVCheckboxVisualElement)}/{nameof(LabeledVCheckboxVisualElement)}")
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