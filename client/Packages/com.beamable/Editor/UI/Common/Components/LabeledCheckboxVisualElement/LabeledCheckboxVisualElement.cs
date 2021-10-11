using System;
using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
using UnityEngine;
using UnityEditor;

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

            readonly UxmlStringAttributeDescription _icon = new UxmlStringAttributeDescription
            { name = "icon", defaultValue = "" };

            readonly UxmlBoolAttributeDescription _flip = new UxmlBoolAttributeDescription
            { name = "flip", defaultValue = false };

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
                    component.Flip = _flip.GetValueFromBag(bag, cc);
                    component.Icon = _icon.GetValueFromBag(bag, cc);
                }
            }
        }

        private Label _label;
        private Image _icon;
        private BeamableCheckboxVisualElement _checkbox;

        private bool Flip { get; set; }
        private string Label { get; set; }
        private string Icon { get; set; }

        public LabeledCheckboxVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledCheckboxVisualElement)}/{nameof(LabeledCheckboxVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _label = Root.Q<Label>("label");
            _label.text = Label;

            _icon = Root.Q<Image>("icon");
            _icon.image = !string.IsNullOrEmpty(Icon) ? (Texture)EditorGUIUtility.Load(Icon): null;

            if (_icon.image.value == null)
                _icon.RemoveFromHierarchy();

            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _checkbox.OnValueChanged -= OnChanged;
            _checkbox.OnValueChanged += OnChanged;
            _checkbox.Refresh();

            if (Flip)
            {
                _icon.SendToBack();
                _checkbox.SendToBack();
            }
        }

        private void OnChanged(bool value)
        {
            OnValueChanged?.Invoke(value);
        }

        public void SetWithoutNotify(bool val) => _checkbox.SetWithoutNotify(val);

        public void SetText(string val) => _label.text = val;

        public void SetFlipState(bool val) => Flip = val;
    }
}