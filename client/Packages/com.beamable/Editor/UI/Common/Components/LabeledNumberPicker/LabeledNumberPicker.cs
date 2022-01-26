using Beamable.Editor.UI.Buss;
using System;
using System.Collections.Generic;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class LabeledNumberPicker : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<LabeledNumberPicker, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
			{ name = "label", defaultValue = "Label" };

			private readonly UxmlIntAttributeDescription _minValue = new UxmlIntAttributeDescription
			{ name = "min", defaultValue = Int32.MinValue };

			private readonly UxmlIntAttributeDescription _maxValue = new UxmlIntAttributeDescription
			{ name = "max", defaultValue = Int32.MaxValue };
			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				if (ve is LabeledNumberPicker component)
				{
					component.Label = _label.GetValueFromBag(bag, cc);
					component.MinValue = _minValue.GetValueFromBag(bag, cc);
					component.MaxValue = _maxValue.GetValueFromBag(bag, cc);
				}
			}
		}

		private LabeledIntegerField _labeledIntegerFieldComponent;
		private Button _button;
		private List<string> _options;
		private Action _onValueChanged;

		public string Value => _labeledIntegerFieldComponent.Value.ToString();
		private int MinValue { get; set; }
		private int MaxValue { get; set; }
		private string Label { get; set; }

		public LabeledNumberPicker() : base($"{BeamableComponentsConstants.COMP_PATH}/{nameof(LabeledNumberPicker)}/{nameof(LabeledNumberPicker)}")
		{
			_options = new List<string>();
		}

		public override void Refresh()
		{
			base.Refresh();

			_labeledIntegerFieldComponent = Root.Q<LabeledIntegerField>("labelAndValue");
			_labeledIntegerFieldComponent.Setup(Label, Int32.Parse(Value), _onValueChanged, MinValue, MaxValue);

			_button = Root.Q<Button>("button");

			ConfigureOptions();
		}

		public void Setup(Action onValueChanged, List<string> options, bool active = true)
		{
			_onValueChanged = onValueChanged;
			SetEnabled(active);
			_options = options;
		}

		public void SetupMinMax(int min, int max)
		{
			MinValue = min;
			MaxValue = max;
		}

		private void ConfigureOptions()
		{
			ContextualMenuManipulator manipulator = new ContextualMenuManipulator(BuildOptions);
			manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_button.clickable.activators.Clear();
			_button.AddManipulator(manipulator);

			if (_options != null && _options.Count > 0)
			{
				SetOption(_options[0]);
			}
		}

		private void BuildOptions(ContextualMenuPopulateEvent evt)
		{
			foreach (string option in _options)
			{
				evt.menu.BeamableAppendAction(option, (pos) =>
				{
					SetOption(option);
				});
			}
		}

		private void SetOption(string value)
		{
			_labeledIntegerFieldComponent.Value = Int32.Parse(value);
			_labeledIntegerFieldComponent.Refresh();
		}

		public void Set(string option) => SetOption(option);
	}
}
