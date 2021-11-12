using System;
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
    public class GenericButtonVisualElement : BeamableVisualElement
    {
        const int DEFAULT_WIDTH = 100;
        const int DEFAULT_HEIGHT = 50;

        public enum ButtonType
        {
            Default,
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

            readonly UxmlStringAttributeDescription _tooltip = new UxmlStringAttributeDescription
            { name = "tooltip", defaultValue = "" };

            readonly UxmlStringAttributeDescription _type = new UxmlStringAttributeDescription
                {name = "type", defaultValue = "default"};

            readonly UxmlStringAttributeDescription _width = new UxmlStringAttributeDescription
                { name = "width", defaultValue = "" };

            readonly UxmlStringAttributeDescription _height = new UxmlStringAttributeDescription
                { name = "height", defaultValue = "" };

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
                    component.Tooltip = _tooltip.GetValueFromBag(bag, cc);

                    string passedType = _type.GetValueFromBag(bag, cc);
                    bool parsed = Enum.TryParse(passedType, true, out ButtonType parsedType);
                    component.Type = parsed ? parsedType : _defaultType;

                    if (int.TryParse(_width.GetValueFromBag(bag, cc), out int width) && int.TryParse(_height.GetValueFromBag(bag, cc), out int height))
                        component.Size = new Vector2(width, height);
                    else
                        component.Size = new Vector2(DEFAULT_WIDTH, DEFAULT_HEIGHT);
                }
            }
        }

        private Button _button;
        private VisualElement _mainVisualElement;
        
        public ButtonType Type { get; set; }
        public string Label { get; set; }
        public string Tooltip { get; set; }
        public Vector2 Size { get; set; }

        public GenericButtonVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(GenericButtonVisualElement)}/{nameof(GenericButtonVisualElement)}")
        {
        }

        public override void Refresh()
        {
            _button = Root.Q<Button>("button");
            _button.text = Label;
            _button.tooltip = Tooltip;
            _button.clickable.clicked += () => { OnClick?.Invoke(); };
            _button.SetSize(Size);

            _mainVisualElement = Root.Q<VisualElement>("mainVisualElement");
            _mainVisualElement.AddToClassList(Type.ToString().ToLower());
        }
    }
}