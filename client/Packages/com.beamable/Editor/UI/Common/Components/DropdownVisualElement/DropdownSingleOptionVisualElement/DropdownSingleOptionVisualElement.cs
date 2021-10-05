using System;
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
    public class DropdownSingleOptionVisualElement : BeamableVisualElement
    {
        private Label _label;
        private string _labelText;
        private VisualElement _root;
        private float _width;
        private float _height;
        private Action<string> _onClick;

        public float Height => _height;

        public new class UxmlFactory : UxmlFactory<DropdownSingleOptionVisualElement, UxmlTraits>
        {
        }
        
        public DropdownSingleOptionVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(DropdownVisualElement)}/{nameof(DropdownSingleOptionVisualElement)}/{nameof(DropdownSingleOptionVisualElement)}")
        {
            _labelText = string.Empty;
        }

        public override void Refresh()
        {
            base.Refresh();
            _root = Root.Q<VisualElement>("mainVisualElement");
            _root.style.height = new StyleLength(_height);
            _root.style.width = new StyleLength(_width);
            _root.style.fontSize = new StyleLength(_height / 2.0f);
            
            _label = Root.Q<Label>("value");
            _label.text = _labelText;

            _root.RegisterCallback<MouseDownEvent>(Clicked);
        }

        private void Clicked(MouseDownEvent evt)
        {
            evt.StopPropagation();
            _onClick?.Invoke(_labelText);
        }

        public DropdownSingleOptionVisualElement Setup(string label, Action<string> onClick, float width, float height)
        {
            _labelText = label;
            _height = height;
            _width = width;

            _onClick = onClick;
            return this;
        }
    }
}