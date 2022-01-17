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
		public new class UxmlFactory : UxmlFactory<BussStyleCardVisualElement, UxmlTraits> { }

		public BussStyleCardVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}") { }

		private VisualElement _styleIdParent;
		private TextElement _styleIdLabel;
		private TextField _styleIdEditField;
		
		private VariableDatabase _variableDatabase;
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;
		
		private VisualElement _properties;
		private VisualElement _editButton;
		private VisualElement _wizardButton;
		private VisualElement _undoButton;
		private VisualElement _cleanAllButton;
		private VisualElement _addVariableButton;
		private VisualElement _addRuleButton;
		private VisualElement _showAllButton;

		public override void Refresh()
		{
			base.Refresh();

			_styleIdParent = Root.Q<VisualElement>("styleIdParent");
			_properties = Root.Q<VisualElement>("properties");

			_editButton = Root.Q<VisualElement>("editButton");
			_wizardButton = Root.Q<VisualElement>("wizardButton");
			_undoButton = Root.Q<VisualElement>("undoButton");
			_cleanAllButton = Root.Q<VisualElement>("cleanAllButton");
			_addVariableButton = Root.Q<VisualElement>("addVariableButton");
			_addRuleButton = Root.Q<VisualElement>("addRuleButton");
			_showAllButton = Root.Q<VisualElement>("showAllButton");
			
			RegisterButtonActions();

			CreateStyleIdLabel();
			CreateProperties();
		}

		protected override void OnDestroy()
		{
			ClearButtonActions();
		}

		private void RegisterButtonActions()
		{
			ClearButtonActions();
			
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
			_editButton?.UnregisterCallback<MouseDownEvent>(EditButtonClicked);
			_wizardButton?.UnregisterCallback<MouseDownEvent>(WizardButtonClicked);
			_undoButton?.UnregisterCallback<MouseDownEvent>(UndoButtonClicked);
			_cleanAllButton?.UnregisterCallback<MouseDownEvent>(CleanAllButtonClicked);
			_addVariableButton?.UnregisterCallback<MouseDownEvent>(AddVariableButtonClicked);
			_addRuleButton?.UnregisterCallback<MouseDownEvent>(AddRuleButtonClicked);
			_showAllButton?.UnregisterCallback<MouseDownEvent>(ShowAllButtonClicked);
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
			Debug.Log("EditButtonClicked");
		}
		
		private void ShowAllButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("ShowAllButtonClicked");
		}

		private void CreateStyleIdLabel()
		{
			_styleIdLabel = new TextElement();
			_styleIdLabel.name = "styleId";
			_styleIdLabel.text = _styleRule.SelectorString;
			_styleIdParent.Add(_styleIdLabel);
			
			_styleIdLabel.RegisterCallback<MouseDownEvent>(StyleIdClicked);
		}

		private void RemoveStyleIdLabel()
		{
			if (_styleIdLabel == null)
			{
				return;
			}

			_styleIdLabel.UnregisterCallback<MouseDownEvent>(StyleIdClicked);
			_styleIdParent.Remove(_styleIdLabel);
			_styleIdLabel = null;
		}

		private void StyleIdClicked(MouseDownEvent evt)
		{
			RemoveStyleIdLabel();
			CreateStyleIdEditField();
		}

		private void CreateStyleIdEditField()
		{
			_styleIdEditField = new TextField();
			_styleIdEditField.name = "styleId";
			_styleIdEditField.value = _styleRule.SelectorString;
			_styleIdEditField.RegisterValueChangedCallback(StyleIdChanged);
			_styleIdParent.Add(_styleIdEditField);
		}

		private void RemoveStyleIdEditField()
		{
			if (_styleIdEditField == null)
			{
				return;
			}

			_styleIdEditField.UnregisterValueChangedCallback(StyleIdChanged);
			_styleIdParent.Remove(_styleIdEditField);
			_styleIdEditField = null;
		}

		private void StyleIdChanged(ChangeEvent<string> evt)
		{
			// TODO: apply change to property
		}

		public void Setup(BussStyleSheet styleSheet, BussStyleRule styleRule, VariableDatabase variableDatabase)
		{
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_variableDatabase = variableDatabase;
			Refresh();
		}

		private void CreateProperties()
		{
			foreach (BussPropertyProvider property in _styleRule.Properties)
			{
				if(property.IsVariable) continue;
				
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(_styleSheet, property, _variableDatabase);
				_properties.Add(element);
			}
		}
	}
}
