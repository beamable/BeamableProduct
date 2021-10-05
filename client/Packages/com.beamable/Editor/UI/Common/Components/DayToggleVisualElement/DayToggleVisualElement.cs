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
    public class DayToggleVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<DayToggleVisualElement, UxmlTraits>
        {
        }

        private VisualElement _checkMark;
        private VisualElement _button;
        private Label _label;
        private string _labelValue;
        
        public bool Selected { get; private set; }
        public string Value { get; private set; }

        public DayToggleVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(DayToggleVisualElement)}/{nameof(DayToggleVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();
            base.Refresh();
            _checkMark = Root.Q<VisualElement>("checkmark");
            _button = Root.Q<VisualElement>("button");
            _label = Root.Q<Label>("label");
            _label.text = _labelValue;
            
            _button.RegisterCallback<MouseDownEvent>(OnClick);

            Render();
        }

        private void Render()
        {
            _checkMark.visible = Selected;
        }

        private void OnClick(MouseDownEvent evt)
        {
            Selected = !Selected;
            Render();
        }

        public void Setup(string label, string option)
        {
            _labelValue = label;
            Value = option;
        }
    }
}