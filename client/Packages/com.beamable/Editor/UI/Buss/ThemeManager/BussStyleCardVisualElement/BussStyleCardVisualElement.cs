using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System.Linq;
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

		private BussStyleRule _styleRule;
		private VisualElement _selectorLabelParent;
		private VisualElement _properties;
		private VisualElement _editButton;
		private VisualElement _wizardButton;
		private VisualElement _undoButton;
		private VisualElement _cleanAllButton;
		private VisualElement _addVariableButton;
		private VisualElement _addRuleButton;
		private VisualElement _showAllButton;
		private TextElement _styleIdLabel;
		private TextField _styleIdEditField;
		private BussSelectorLabelVisualElement _selectorLabelComponent;

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
			Debug.Log("AddRuleButtonClicked");
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

		public void Setup(BussStyleRule styleRule)
		{
			_styleRule = styleRule;
			Refresh();
		}

		private void CreateProperties()
		{
			foreach (BussPropertyProvider property in _styleRule.Properties)
			{
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(property);
				_properties.Add(element);
			}
		}
	}
}
