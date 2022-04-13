using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class BussSelectorLabelVisualElement : BeamableBasicVisualElement
	{
		private TextField _editableLabel;
		private BussStyleRule _styleRule;
		private BussStyleSheet _styleSheet;
		private List<GenericMenuCommand> _commands;

		public BussSelectorLabelVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/BussStyleCardVisualElement/BussSelectorLabelVisualElement/BussSelectorLabelVisualElement.uss") { }

		public void Setup(BussStyleRule styleRule, BussStyleSheet styleSheet, List<GenericMenuCommand> commands)
		{
			base.Init();

			_styleRule = styleRule;
			_styleSheet = styleSheet;
			_commands = commands;

			Refresh();
		}

		private new void Refresh()
		{
			Root.Clear();

			_editableLabel = new TextField();
			_editableLabel.name = "styleId";
			_editableLabel.value = _styleRule.SelectorString;
			_editableLabel.RegisterValueChangedCallback(StyleIdChanged);
			_editableLabel.RegisterCallback<KeyDownEvent>(KeyboardPressed);
			

			Root.Add(_editableLabel);

			TextElement separator01 = new TextElement();
			separator01.name = "separator";
			separator01.text = "|";
			Root.Add(separator01);

			TextElement styleSheetLabel = new TextElement();
			styleSheetLabel.name = "styleSheetLabel";
			styleSheetLabel.text = $"{_styleSheet.name}";
			styleSheetLabel.RegisterCallback<MouseDownEvent>(OnStyleSheetClicked);
			Root.Add(styleSheetLabel);

			if (_styleSheet.IsReadOnly)
			{
				TextElement separator02 = new TextElement();
				separator02.name = "separator";
				separator02.text = "|";
				Root.Add(separator02);

				TextElement readonlyLabel = new TextElement();
				readonlyLabel.name = "readonlyLabel";
				readonlyLabel.text = "readonly";
				Root.Add(readonlyLabel);
			}
		}

		private void KeyboardPressed(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.Return)
			{
				Focus();
			}
		}

		private void OnStyleSheetClicked(MouseDownEvent evt)
		{
			if (_styleSheet.IsReadOnly)
			{
				return;
			}

			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in _commands)
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
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
