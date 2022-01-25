using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using Beamable.UI.BUSS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss
{
	public class AddPropertiesVisualElement : BeamableVisualElement
	{
		private Action<BussStyleRule> _onSelectorAdded;

		private LabeledTextField _selectorName;
		private PrimaryButtonVisualElement _confirmButton;
		private ScrollView _rulesContainer;

		private readonly Dictionary<string, LabeledCheckboxVisualElement> _rules =
			new Dictionary<string, LabeledCheckboxVisualElement>();

		private BussStyleSheet _currentSelectedStyleSheet;

		public AddPropertiesVisualElement(Action<BussStyleRule> onSelectorAdded) : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/AddStyleWindow/{nameof(AddPropertiesVisualElement)}/{nameof(AddPropertiesVisualElement)}")
		{
			_onSelectorAdded = onSelectorAdded;
		}

		public override void Refresh()
		{
			base.Refresh();

			Root.parent.parent.style.flexGrow = 1;

			_selectorName = Root.Q<LabeledTextField>("styleName");
			_selectorName.Setup("Style name", string.Empty, OnValidate);
			_selectorName.Refresh();

			_rulesContainer = Root.Q<ScrollView>("propertiesContainer");

			_confirmButton = Root.Q<PrimaryButtonVisualElement>("confirmButton");
			_confirmButton.Button.clickable.clicked += HandleConfirmButton;
			_confirmButton.Disable();

			GenericButtonVisualElement cancelButton = Root.Q<GenericButtonVisualElement>("cancelButton");
			cancelButton.OnClick += AddStyleWindow.CloseWindow;

			List<string> folders = new List<string>{"Assets"};

#if BEAMABLE_DEVELOPER
			folders.Add("Packages");
#endif

			List<BussStyleSheet> styleSheets = Helper.FindAssets<BussStyleSheet>("t:BussStyleSheet", folders.ToArray());
			LabeledDropdownVisualElement selectStyleSheet = Root.Q<LabeledDropdownVisualElement>("selectStyleSheet");
			selectStyleSheet.Setup(styleSheets.Select(x => x.name).ToList(), index =>
			{
				_currentSelectedStyleSheet = styleSheets[index];
				OnValidate();
			});

			selectStyleSheet.Refresh();

			ListAllRules();
			OnValidate();
		}

		private void ListAllRules()
		{
			foreach (var key in BussStyle.Keys.OrderBy(x => x))
			{
				var rule = new LabeledCheckboxVisualElement(key, true);
				rule.Refresh();
				_rules.Add(key, rule);
				_rulesContainer.Add(rule);
			}
		}

		private void HandleConfirmButton()
		{
			var rules = new List<BussPropertyProvider>();
			foreach (var kvp in _rules)
			{
				var checkboxVisualElement = kvp.Value;
				if (!checkboxVisualElement.Value)
					continue;

				rules.Add(BussPropertyProvider.Create(kvp.Key, BussStyle.GetDefaultValue(kvp.Key).CopyProperty()));
			}

			var selector = BussStyleRule.Create(_selectorName.Value, rules);
			_currentSelectedStyleSheet.Styles.Add(selector);
			_onSelectorAdded?.Invoke(selector);
			AssetDatabase.SaveAssets();
			AddStyleWindow.CloseWindow();
		}

		private void OnValidate()
		{
			if (string.IsNullOrWhiteSpace(_selectorName.Value))
				ChangeButtonState(false,
				                  "Selector name cannot be empty or white space");
			else
				ChangeButtonState(true);

			foreach (var localStyle in _currentSelectedStyleSheet.Styles)
				if (localStyle.SelectorString == _selectorName.Value)
					ChangeButtonState(false,
					                  $"Selector '{_selectorName.Value}' already exists in '{_currentSelectedStyleSheet.name}' BUSS style sheet");
		}

		private void ChangeButtonState(bool isEnabled, string tooltip = "")
		{
			_confirmButton.tooltip = tooltip;
			if (isEnabled)
				_confirmButton.Enable();
			else
				_confirmButton.Disable();
		}
	}
}
