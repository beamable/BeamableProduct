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
    public class GenericButtonVisualElement : BeamableVisualElement
    {
        public enum ButtonType
        {
            Confirm,
            Cancel
        }
        
        public new class UxmlFactory : UxmlFactory<GenericButtonVisualElement, UxmlTraits>
        {
        }

        public event Action OnClick;

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private ButtonType _defaultType = ButtonType.Confirm;
            
            readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
                {name = "label", defaultValue = "Label"};
            
            readonly UxmlStringAttributeDescription _type = new UxmlStringAttributeDescription
                {name = "type", defaultValue = "confirm"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is GenericButtonVisualElement component)
                {
                    component.Label = _label.GetValueFromBag(bag, cc);

                    string passedType = _type.GetValueFromBag(bag, cc);
                    bool parsed = Enum.TryParse(passedType, true, out ButtonType parsedType);
                    component.Type = parsed ? parsedType : _defaultType;
                }
            }
        }

        private Label _label;
        private Button _button;
        private VisualElement _mainVisualElement;
        
        public ButtonType Type { get; set; }
        public string Label { get; set; }

        public GenericButtonVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(GenericButtonVisualElement)}/{nameof(GenericButtonVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

             _label = Root.Q<Label>("label");
             _label.text = Label;

            _button = Root.Q<Button>("button");
            _button.clickable.clicked += () => { OnClick?.Invoke(); };

            _mainVisualElement = Root.Q<VisualElement>("mainVisualElement");
            _mainVisualElement.AddToClassList(Type.ToString().ToLower());
        }
    }
}