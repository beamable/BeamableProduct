using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.UI.Buss;
using Editor.UI.BUSS.ThemeManager;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class BussStyleCardVisualElement : BeamableVisualElement
	{
		private BussSelectorLabelVisualElement _selectorLabelComponent;
		private VisualElement _styleIdParent;
		private VisualElement _selectorLabelParent;
		private VisualElement _variables;
		private VisualElement _properties;
		private VisualElement _colorBlock;
		private VisualElement _removeButton;
		private VisualElement _editButton;
		private VisualElement _wizardButton;
		private VisualElement _undoButton;
		private VisualElement _cleanAllButton;
		private VisualElement _addVariableButton;
		private VisualElement _addRuleButton;
		private VisualElement _showAllButton;
		private TextElement _showAllButtonText;
		private TextElement _styleIdLabel;
		private TextField _styleIdEditField;

		private VariableDatabase _variableDatabase;
		private BussStyleSheet _styleSheet;
		private BussElementHierarchyVisualElement _navigationWindow;

		public BussStyleRule StyleRule
		{
			get;
			private set;
		}

		public BussStyleCardVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_selectorLabelParent = Root.Q<VisualElement>("selectorLabelParent");
			_variables = Root.Q<VisualElement>("variables");
			_properties = Root.Q<VisualElement>("properties");
			_colorBlock = Root.Q<VisualElement>("colorBlock");

			_removeButton = Root.Q<VisualElement>("removeButton");
			_editButton = Root.Q<VisualElement>("editButton");
			_wizardButton = Root.Q<VisualElement>("wizardButton");
			_undoButton = Root.Q<VisualElement>("undoButton");
			_cleanAllButton = Root.Q<VisualElement>("cleanAllButton");
			_addVariableButton = Root.Q<VisualElement>("addVariableButton");
			_addRuleButton = Root.Q<VisualElement>("addRuleButton");
			_showAllButton = Root.Q<VisualElement>("showAllButton");
			_showAllButtonText = Root.Q<TextElement>("showAllButtonText");

			_navigationWindow.SelectionChanged -= OnSelectionChanged;
			_navigationWindow.SelectionChanged += OnSelectionChanged;

			RegisterButtonActions();

			CreateSelectorLabel();
			CreateProperties();

			_styleSheet.Change -= Refresh;
			_styleSheet.Change += Refresh;

			_removeButton.SetHidden(!StyleRule.EditMode);
			UpdateShowAllStatus();
		}

		public void Setup(BussStyleSheet styleSheet,
		                  BussStyleRule styleRule,
		                  VariableDatabase variableDatabase,
		                  BussElementHierarchyVisualElement navigationWindow)
		{
			_styleSheet = styleSheet;
			StyleRule = styleRule;
			_variableDatabase = variableDatabase;
			_navigationWindow = navigationWindow;

			_styleSheet.Change += Refresh;

			Refresh();
		}

		protected override void OnDestroy()
		{
			_styleSheet.Change -= Refresh;
			ClearButtonActions();

			if (_navigationWindow != null)
			{
				_navigationWindow.SelectionChanged -= OnSelectionChanged;
			}
		}

		private void RegisterButtonActions()
		{
			ClearButtonActions();

			_removeButton?.RegisterCallback<MouseDownEvent>(RemoveButtonClicked);
			_editButton?.RegisterCallback<MouseDownEvent>(EditButtonClicked);
			_wizardButton?.RegisterCallback<MouseDownEvent>(WizardButtonClicked);
			_undoButton?.RegisterCallback<MouseDownEvent>(UndoButtonClicked);
			_cleanAllButton?.RegisterCallback<MouseDownEvent>(CleanAllButtonClicked);
			_addVariableButton?.RegisterCallback<MouseDownEvent>(AddVariableButtonClicked);
			_addRuleButton?.RegisterCallback<MouseDownEvent>(AddRuleButtonClicked);
			_showAllButton?.RegisterCallback<MouseDownEvent>(ShowAllButtonClicked);
		}

		private void ClearButtonActions()
		{
			_removeButton?.UnregisterCallback<MouseDownEvent>(RemoveButtonClicked);
			_editButton?.UnregisterCallback<MouseDownEvent>(EditButtonClicked);
			_wizardButton?.UnregisterCallback<MouseDownEvent>(WizardButtonClicked);
			_undoButton?.UnregisterCallback<MouseDownEvent>(UndoButtonClicked);
			_cleanAllButton?.UnregisterCallback<MouseDownEvent>(CleanAllButtonClicked);
			_addVariableButton?.UnregisterCallback<MouseDownEvent>(AddVariableButtonClicked);
			_addRuleButton?.UnregisterCallback<MouseDownEvent>(AddRuleButtonClicked);
			_showAllButton?.UnregisterCallback<MouseDownEvent>(ShowAllButtonClicked);
		}

		private void RemoveButtonClicked(MouseDownEvent evt)
		{
			_styleSheet.RemoveStyle(StyleRule);
			AssetDatabase.SaveAssets();
		}

		private void AddRuleButtonClicked(MouseDownEvent evt)
		{
			var keys = new HashSet<string>();
			foreach (var propertyProvider in StyleRule.Properties)
			{
				keys.Add(propertyProvider.Key);
			}

			var context = new GenericMenu();

			foreach (var key in BussStyle.Keys)
			{
				if (keys.Contains(key)) continue;
				context.AddItem(new GUIContent(key), false, () =>
				{
					StyleRule.Properties.Add(
						BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty()));
					AssetDatabase.SaveAssets();
					_styleSheet.TriggerChange();
				});
			}

			context.ShowAsContext();
		}

		private void AddVariableButtonClicked(MouseDownEvent evt)
		{
			var window = NewVariableWindow.ShowWindow();
			window?.Init((key, property) =>
			{
				StyleRule.TryAddProperty(key, property, out _);
				AssetDatabase.SaveAssets();
				_styleSheet.TriggerChange();
			});
		}

		private void CleanAllButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("CleanAllButtonClicked");
		}

		private void UndoButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("UndoButtonClicked");
		}

		private void WizardButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("WizardButtonClicked");
		}

		private void EditButtonClicked(MouseDownEvent evt)
		{
			StyleRule.EditMode = !StyleRule.EditMode;
			Refresh();
		}

		private void ShowAllButtonClicked(MouseDownEvent evt)
		{
			StyleRule.ShowAllMode = !StyleRule.ShowAllMode;
			UpdateShowAllStatus();
		}

		private void UpdateShowAllStatus()
		{
			const string showAllProperty = "showAllProperties";
			EnableInClassList(showAllProperty, StyleRule.ShowAllMode);
			_showAllButtonText.text = StyleRule.ShowAllMode ? "Hide All" : "Show All";
		}

		private void CreateSelectorLabel()
		{
			_selectorLabelComponent?.Destroy();
			_selectorLabelParent.Clear();

			_selectorLabelComponent = new BussSelectorLabelVisualElement();
			_selectorLabelComponent.Setup(StyleRule);
			_selectorLabelParent.Add(_selectorLabelComponent);
		}

		private void CreateProperties()
		{
			foreach (BussPropertyProvider property in StyleRule.Properties)
			{
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(_styleSheet, StyleRule, property, _variableDatabase);
				element.AddToClassList("exists");
				(property.IsVariable ? _variables : _properties).Add(element);
			}

			var restPropertyKeys = BussStyle.Keys.Where(s => StyleRule.Properties.All(provider => provider.Key != s));
			foreach (var key in restPropertyKeys)
			{
				var propertyProvider = BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty());
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.OnValueChanged = () =>
				{
					StyleRule.TryAddProperty(key, propertyProvider.GetProperty(), out _);
					Refresh();
				};
				element.Setup(null, StyleRule, propertyProvider, _variableDatabase);
				element.AddToClassList("doesntExists");
				_properties.Add(element);
			}
		}

		// TODO: change this, card should be setup/refreshed by it's parent
		private void OnSelectionChanged(GameObject gameObject)
		{
			if (_colorBlock == null || gameObject == null) return;

			var active = false;
			var bussElement = gameObject.GetComponent<BussElement>();
			if (bussElement != null && StyleRule.Selector != null)
			{
				active = StyleRule.Selector.CheckMatch(bussElement);
			}

			_colorBlock.EnableInClassList("active", active);
		}
	}
}
