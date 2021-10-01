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
    public class HourPickerVisualElement : BeamableVisualElement
    {
        private LabeledNumberPicker _hourPicker;
        private LabeledNumberPicker _minutePicker;
        private LabeledNumberPicker _secondPicker;

        public new class UxmlFactory : UxmlFactory<HourPickerVisualElement, UxmlTraits>
        {
        }

        public HourPickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(HourPickerVisualElement)}/{nameof(HourPickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _hourPicker = Root.Q<LabeledNumberPicker>("hourPicker");
            _hourPicker.Refresh();
            
            _minutePicker = Root.Q<LabeledNumberPicker>("minutePicker");
            _minutePicker.Refresh();
            
            _secondPicker = Root.Q<LabeledNumberPicker>("secondPicker");
            _secondPicker.Refresh();
        }
    }
}