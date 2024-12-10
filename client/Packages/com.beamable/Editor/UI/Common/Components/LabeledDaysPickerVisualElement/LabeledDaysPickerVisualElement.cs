using Beamable.Editor.UI.Validation;
using System.Collections.Generic;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants;

namespace Beamable.Editor.UI.Components
{
	#if UNITY_6000_0_OR_NEWER
	[UxmlElement]
	#endif
	public partial class LabeledDaysPickerVisualElement : ValidableVisualElement<int>
	{
#if UNITY_6000_0_OR_NEWER
		[UxmlAttribute]
		public string label { get; set; } = "Label";
#else

		public new class UxmlFactory : UxmlFactory<LabeledDaysPickerVisualElement, UxmlTraits>
		{
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
				if (ve is LabeledDaysPickerVisualElement component)
				{
					component.Label = _label.GetValueFromBag(bag, cc);
				}
			}
		}
#endif

		private Label _label;

		public DaysPickerVisualElement DaysPicker { get; private set; }
		public string Label { get; set; }

		public LabeledDaysPickerVisualElement() : base(
			$"{Directories.COMMON_COMPONENTS_PATH}/{nameof(LabeledDaysPickerVisualElement)}/{nameof(LabeledDaysPickerVisualElement)}")
		{
		}

		public override void Refresh()
		{
			base.Refresh();

			_label = Root.Q<Label>("label");
			_label.text = Label;

			DaysPicker = Root.Q<DaysPickerVisualElement>("daysPicker");
			DaysPicker.OnValueChanged = OnChanged;
			DaysPicker.Refresh();
		}

		private void OnChanged(List<string> options)
		{
			InvokeValidationCheck(options.Count);
		}

		public void SetSelectedDays(IEnumerable<string> dayCodes) => DaysPicker.SetSelectedDays(dayCodes);
	}
}
