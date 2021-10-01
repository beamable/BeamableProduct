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
    public class DatePickerVisualElement : BeamableVisualElement
    {
        private LabeledNumberPicker _yearPicker;
        private LabeledNumberPicker _monthPicker;
        private LabeledNumberPicker _dayPicker;

        public new class UxmlFactory : UxmlFactory<DatePickerVisualElement, UxmlTraits>
        {
        }

        public DatePickerVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(DatePickerVisualElement)}/{nameof(DatePickerVisualElement)}")
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _yearPicker = Root.Q<LabeledNumberPicker>("yearPicker");
            _yearPicker.Refresh();
            
            _monthPicker = Root.Q<LabeledNumberPicker>("monthPicker");
            _monthPicker.Refresh();
            
            _dayPicker = Root.Q<LabeledNumberPicker>("dayPicker");
            _dayPicker.Refresh();
        }
    }
}