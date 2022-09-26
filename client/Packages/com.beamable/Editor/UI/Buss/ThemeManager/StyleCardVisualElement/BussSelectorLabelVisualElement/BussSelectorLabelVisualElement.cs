using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
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
		private TextElement _styleSheetLabel;
		private BussStyleRule _styleRule;
		private BussStyleSheet _styleSheet;

		private Func<List<GenericMenuCommand>> _refreshCommands;

		public BussSelectorLabelVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StyleCardVisualElement)}/{nameof(BussSelectorLabelVisualElement)}/{nameof(BussSelectorLabelVisualElement)}.uss") { }

		public void Setup(BussStyleRule styleRule,
		                  BussStyleSheet styleSheet,
		                  Func<List<GenericMenuCommand>> refreshCommands)
		{
			base.Init();

			_styleRule = styleRule;
			_styleSheet = styleSheet;
			_refreshCommands = refreshCommands;

			Refresh();
		}

		private new void Refresh()
		{
			Root.Clear();

#if BEAMABLE_DEVELOPER
			_editableLabel = new TextField();
			_editableLabel.AddToClassList("interactable");
			_editableLabel.value = _styleRule.SelectorString;
			_editableLabel.RegisterValueChangedCallback(StyleIdChanged);
			_editableLabel.RegisterCallback<KeyDownEvent>(KeyboardPressed);
			Root.Add(_editableLabel);
#else
			if (_styleSheet.IsReadOnly)
			{
				TextElement styleId = new TextElement();
				styleId.text = _styleRule.SelectorString;
				Root.Add(styleId);
			}
			else
			{
				_editableLabel = new TextField();
				if (!_styleSheet.IsReadOnly)
				{
					_editableLabel.AddToClassList("interactable");
				}

				_editableLabel.value = _styleRule.SelectorString;
				_editableLabel.RegisterValueChangedCallback(StyleIdChanged);
				_editableLabel.RegisterCallback<KeyDownEvent>(KeyboardPressed);
				Root.Add(_editableLabel);
			}
#endif

			TextElement separator01 = new TextElement {name = "separator", text = "|"};
			Root.Add(separator01);

			_styleSheetLabel = new TextElement();
			_styleSheetLabel.AddToClassList("interactable");

			_styleSheetLabel.name = "styleSheetName";
			_styleSheetLabel.text = $"{_styleSheet.name}";
			_styleSheetLabel.RegisterCallback<MouseDownEvent>(OnStyleSheetClicked);
			Root.Add(_styleSheetLabel);

			if (_styleSheet.IsReadOnly)
			{
				TextElement separator02 = new TextElement {name = "separator", text = "|"};
				Root.Add(separator02);

				TextElement readonlyLabel = new TextElement {text = "readonly"};
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
			GenericMenu context = new GenericMenu();

			List<GenericMenuCommand> commands = _refreshCommands.Invoke();

			foreach (GenericMenuCommand command in commands)
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		protected override void OnDestroy()
		{
			_editableLabel.UnregisterValueChangedCallback(StyleIdChanged);
			_styleSheetLabel.UnregisterCallback<MouseDownEvent>(OnStyleSheetClicked);
		}

		private void StyleIdChanged(ChangeEvent<string> evt)
		{
			_styleRule.SelectorString = evt.newValue;
			_styleSheet.TriggerChange();
		}
	}
}
