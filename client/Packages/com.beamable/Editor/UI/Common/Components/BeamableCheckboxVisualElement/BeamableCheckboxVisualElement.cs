using System;
using System.Collections.Generic;
using Beamable.Editor.Realms;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Common.Models;
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
    public class BeamableCheckboxVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<BeamableCheckboxVisualElement, UxmlTraits>
        {
        }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
                {name = "custom-text", defaultValue = "nada"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var self = ve as BeamableCheckboxVisualElement;
            }
        }
        
        public event Action<bool> OnValueChanged;

        public bool Value
        {
            get => _value;
            set
            {
                SetWithoutNotify(value);
                OnValueChanged?.Invoke(value);
            }
        }

        private bool _value;

        private VisualElement _onNotifier;
        private Button _button;
        
        public BeamableCheckboxVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(BeamableCheckboxVisualElement)}/{nameof(BeamableCheckboxVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();
            _onNotifier = Root.Q<VisualElement>("onNotifier");
            _button = Root.Q<Button>("checkboxButton");
            _button.clickable.clicked += () => Value = !_value;;
            UpdateLook();
        }

        public void SetWithoutNotify(bool value)
        {
            _value = value;
            UpdateLook();
        }

        void UpdateLook()
        {
            _onNotifier.visible = Value;
        }
    }
}