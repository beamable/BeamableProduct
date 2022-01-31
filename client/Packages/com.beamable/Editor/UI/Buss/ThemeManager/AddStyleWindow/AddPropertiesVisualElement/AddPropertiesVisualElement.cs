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
		private readonly Action<BussStyleRule> _onSelectorAdded;
		private readonly List<BussStyleSheet> _styleSheets;

		private LabeledTextField _selectorName;
		private PrimaryButtonVisualElement _confirmButton;
		private ScrollView _rulesContainer;

		private readonly Dictionary<string, LabeledCheckboxVisualElement> _rules =
			new Dictionary<string, LabeledCheckboxVisualElement>();

		private BussStyleSheet _currentSelectedStyleSheet;

		public AddPropertiesVisualElement(Action<BussStyleRule> onSelectorAdded, List<BussStyleSheet> styleSheets) :
			base(
				$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/AddStyleWindow/{nameof(AddPropertiesVisualElement)}/{nameof(AddPropertiesVisualElement)}")
		{
			_onSelectorAdded = onSelectorAdded;
			_styleSheets = styleSheets;
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

			LabeledDropdownVisualElement selectStyleSheet = Root.Q<LabeledDropdownVisualElement>("selectStyleSheet");

			List<BussStyleSheet> allStyleSheets = Helper.FindAssets<BussStyleSheet>("t:BussStyleSheet", new[]
			{
				"Assets",
#if BEAMABLE_DEVELOPER
				"Packages"
#endif
			});

#if BEAMABLE_DEVELOPER
			List<BussStyleSheet> styleSheets = new List<BussStyleSheet>(allStyleSheets);
#else
			List<BussStyleSheet> styleSheets = new List<BussStyleSheet>();
			styleSheets.AddRange(_styleSheets.Where(bussStyleSheet => allStyleSheets.Contains(bussStyleSheet)));
#endif

			List<string> labels = styleSheets.Select(x => x.name).ToList();

			selectStyleSheet.Setup(labels, index =>
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
			foreach (string key in BussStyle.Keys.OrderBy(x => x))
			{
				LabeledCheckboxVisualElement rule = new LabeledCheckboxVisualElement(key, true);
				rule.Refresh();
				_rules.Add(key, rule);
				_rulesContainer.Add(rule);
			}
		}

		private void HandleConfirmButton()
		{
			List<BussPropertyProvider> rules = new List<BussPropertyProvider>();
			foreach (KeyValuePair<string, LabeledCheckboxVisualElement> kvp in _rules)
			{
				LabeledCheckboxVisualElement checkboxVisualElement = kvp.Value;
				if (!checkboxVisualElement.Value)
					continue;

				rules.Add(BussPropertyProvider.Create(kvp.Key, BussStyle.GetDefaultValue(kvp.Key).CopyProperty()));
			}

			BussStyleRule selector = BussStyleRule.Create(_selectorName.Value, rules);
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

			if (_currentSelectedStyleSheet != null)
			{
				foreach (BussStyleRule localStyle in _currentSelectedStyleSheet.Styles)
					if (localStyle.SelectorString == _selectorName.Value)
						ChangeButtonState(false,
						                  $"Selector '{_selectorName.Value}' already exists in '{_currentSelectedStyleSheet.name}' BUSS style sheet");
			}
			else
			{
				ChangeButtonState(false,
				                  "Buss style sheet scriptable config doesn't exist");
			}
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
