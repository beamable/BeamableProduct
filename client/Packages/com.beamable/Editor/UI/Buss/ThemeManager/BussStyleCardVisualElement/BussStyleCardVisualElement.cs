using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using Editor.UI.BUSS.ThemeManager;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class BussStyleCardVisualElement : BeamableVisualElement
	{
		public enum MODE
		{
			NORMAL,
			EDIT
		}

		private BussSelectorLabelVisualElement _selectorLabelComponent;
		private VisualElement _styleIdParent;
		private VisualElement _selectorLabelParent;
		private VisualElement _properties;
		private VisualElement _removeButton;
		private VisualElement _editButton;
		private VisualElement _wizardButton;
		private VisualElement _undoButton;
		private VisualElement _cleanAllButton;
		private VisualElement _addVariableButton;
		private VisualElement _addRuleButton;
		private VisualElement _showAllButton;
		private TextElement _styleIdLabel;
		private TextField _styleIdEditField;

		private VariableDatabase _variableDatabase;
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;
		private MODE _currentMode;

		public BussStyleCardVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}")
		{
			_currentMode = MODE.NORMAL;
		}

		public override void Refresh()
		{
			base.Refresh();

			_selectorLabelParent = Root.Q<VisualElement>("selectorLabelParent");
			_properties = Root.Q<VisualElement>("properties");

			_removeButton = Root.Q<VisualElement>("removeButton");
			_editButton = Root.Q<VisualElement>("editButton");
			_wizardButton = Root.Q<VisualElement>("wizardButton");
			_undoButton = Root.Q<VisualElement>("undoButton");
			_cleanAllButton = Root.Q<VisualElement>("cleanAllButton");
			_addVariableButton = Root.Q<VisualElement>("addVariableButton");
			_addRuleButton = Root.Q<VisualElement>("addRuleButton");
			_showAllButton = Root.Q<VisualElement>("showAllButton");
			
			RegisterButtonActions();

			CreateSelectorLabel();
			CreateProperties();
			
			_removeButton.SetVisibility(_currentMode == MODE.EDIT);
		}

		public void Setup(BussStyleSheet styleSheet, BussStyleRule styleRule, VariableDatabase variableDatabase)
		{
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_variableDatabase = variableDatabase;
			
			Refresh();
		}

		protected override void OnDestroy()
		{
			ClearButtonActions();
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
			Debug.Log("RemoveButtonClicked");
		}

		private void AddRuleButtonClicked(MouseDownEvent evt)
		{
			var keys = new HashSet<string>();
			foreach (var propertyProvider in _styleRule.Properties)
			{
				keys.Add(propertyProvider.Key);
			}
			var context = new GenericMenu();

			foreach (var key in BussStyle.Keys) {
				if (keys.Contains(key)) continue;
				context.AddItem(new GUIContent(key), false, () => {
					_styleRule.Properties.Add(BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty()));
					AssetDatabase.SaveAssets();
					_styleSheet.TriggerChange();
				});
			}
			
			context.ShowAsContext();
		}

		private void AddVariableButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("AddVariableButtonClicked");
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
			_currentMode = _currentMode == MODE.NORMAL ? _currentMode = MODE.EDIT : _currentMode = MODE.NORMAL;
			Refresh();
		}

		private void ShowAllButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("ShowAllButtonClicked");
		}

		private void CreateSelectorLabel()
		{
			_selectorLabelComponent?.Destroy();
			_selectorLabelParent.Clear();
			
			_selectorLabelComponent = new BussSelectorLabelVisualElement();
			_selectorLabelComponent.Setup(_currentMode, _styleRule);
			_selectorLabelParent.Add(_selectorLabelComponent);
		}

		private void CreateProperties()
		{
			foreach (BussPropertyProvider property in _styleRule.Properties)
			{
				if(property.IsVariable) continue;
				
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(_styleSheet, property, _variableDatabase, _currentMode);
				_properties.Add(element);
			}
		}
	}
}
