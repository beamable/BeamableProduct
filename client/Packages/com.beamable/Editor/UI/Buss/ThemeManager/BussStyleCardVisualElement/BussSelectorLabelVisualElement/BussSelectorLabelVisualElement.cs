using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Text;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.BeamableConstants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class BussSelectorLabelVisualElement : BeamableBasicVisualElement
	{
		private TextField _editableLabel;
		private BussStyleRule _styleRule;
		private BussStyleSheet _styleSheet;

		public event Action OnChangeSubmit;

		public BussSelectorLabelVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/BussStyleCardVisualElement/BussSelectorLabelVisualElement/BussSelectorLabelVisualElement.uss")
		{ }

		public void Setup(BussStyleRule styleRule, BussStyleSheet styleSheet, bool editMode)
		{
			base.Init();

			_styleRule = styleRule;
			_styleSheet = styleSheet;

			if (!editMode)
			{
				TextElement textLabel = new TextElement();
				textLabel.name = "styleId";

				StringBuilder label = new StringBuilder();
				label.Append($"{styleRule.SelectorString} ({styleSheet.name})");

				if (styleSheet.IsReadOnly)
				{
					label.Append(" - readonly");
				}

				textLabel.text = label.ToString();
				Root.Add(textLabel);
			}
			else
			{
				_editableLabel = new TextField();
				_editableLabel.name = "styleId";
				_editableLabel.value = styleRule.SelectorString;
				_editableLabel.RegisterValueChangedCallback(StyleIdChanged);
				_editableLabel.RegisterCallback<KeyDownEvent>(evt =>
				{
					if (evt.keyCode == KeyCode.Return)
					{
						OnChangeSubmit?.Invoke();
					}
				});
				Root.Add(_editableLabel);
			}
		}

		protected override void OnDestroy()
		{
			_editableLabel.UnregisterValueChangedCallback(StyleIdChanged);
		}

		private void StyleIdChanged(ChangeEvent<string> evt)
		{
			_styleRule.SelectorString = evt.newValue;
			_styleSheet.TriggerChange();
		}
	}
}
