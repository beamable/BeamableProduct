using System.Collections.Generic;
using System.Linq;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;
using static Beamable.Common.Constants;

namespace Beamable.Editor.UI.Components
{
	public class StyleCardVisualElement : BeamableVisualElement
	{
		private readonly StyleCardModel _model;

		// private readonly List<StylePropertyVisualElement> _spawnedProperties = new List<StylePropertyVisualElement>();
		private VisualElement _addRuleButton;
		private VisualElement _addVariableButton;
		private VisualElement _cleanAllButton;
		private VisualElement _colorBlock;
		private VisualElement _optionsButton;
		private VisualElement _propertiesParent;
		private BussSelectorLabelVisualElement _selectorLabelComponent;
		private VisualElement _selectorLabelParent;
		private VisualElement _showAllButton;
		private TextElement _showAllButtonText;
		private VisualElement _sortButton;
		private VisualElement _undoButton;
		private VisualElement _variablesParent;

		public StyleCardVisualElement(StyleCardModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StyleCardVisualElement)}/{nameof(StyleCardVisualElement)}")
		{
			_model = model;
		}

		public override void Refresh()
		{
			base.Refresh();

			_selectorLabelParent = Root.Q<VisualElement>("selectorLabelParent");
			_variablesParent = Root.Q<VisualElement>("variables");
			_propertiesParent = Root.Q<VisualElement>("properties");
			_colorBlock = Root.Q<VisualElement>("colorBlock");

			_optionsButton = Root.Q<VisualElement>("optionsButton");
			_optionsButton.tooltip = Tooltips.Buss.OPTIONS;

			_undoButton = Root.Q<VisualElement>("undoButton");
			_undoButton.tooltip = Tooltips.Buss.UNDO;

			_cleanAllButton = Root.Q<VisualElement>("cleanAllButton");
			_cleanAllButton.tooltip = Tooltips.Buss.ERASE_ALL_STYLE;

			_addVariableButton = Root.Q<VisualElement>("addVariableButton");
			_addRuleButton = Root.Q<VisualElement>("addRuleButton");
			_showAllButton = Root.Q<VisualElement>("showAllButton");
			_sortButton = Root.Q<VisualElement>("sortButton");

			_showAllButtonText = Root.Q<TextElement>("showAllButtonText");

			RegisterButtonActions();
			CreateSelectorLabel();
			RefreshProperties();
			UpdateShowAllStatus();
			RefreshButtons();
			_colorBlock.EnableInClassList("active", _model.IsSelected);
			_model.Change += OnChange;
		}

		protected override void OnDestroy()
		{
			_model.Change -= OnChange;
			ClearButtonActions();
		}

		private void ClearButtonActions()
		{
			_undoButton?.UnregisterCallback<MouseDownEvent>(_model.UndoButtonClicked);
			_cleanAllButton?.UnregisterCallback<MouseDownEvent>(_model.ClearAllButtonClicked);
			_addVariableButton?.UnregisterCallback<MouseDownEvent>(_model.AddVariableButtonClicked);
			_addRuleButton?.UnregisterCallback<MouseDownEvent>(_model.AddRuleButtonClicked);
			_showAllButton?.UnregisterCallback<MouseDownEvent>(_model.ShowAllButtonClicked);
			_sortButton?.UnregisterCallback<MouseDownEvent>(_model.SortButtonClicked);
			_optionsButton?.UnregisterCallback<MouseDownEvent>(_model.OptionsButtonClicked);
		}

		private void ClearSpawnedProperties()
		{
			while (_propertiesParent.Children().Count() > 1)
			{
				var currentCount = _propertiesParent.Children().Count();
				_propertiesParent.RemoveAt(currentCount - 1);
			}
		}

		private void CreateSelectorLabel()
		{
			_selectorLabelComponent?.Destroy();
			_selectorLabelParent.Clear();

			_selectorLabelComponent = new BussSelectorLabelVisualElement();

			_selectorLabelComponent.Setup(_model.StyleRule, _model.StyleSheet, _model.PrepareCommands);
			_selectorLabelParent.Add(_selectorLabelComponent);
		}

		private void OnChange()
		{
			RefreshProperties();
			UpdateShowAllStatus();
			RefreshButtons();
		}

		private void RefreshButtons()
		{
			_undoButton.SetEnabled(_model.IsWritable);
			_cleanAllButton.SetEnabled(_model.IsWritable);
			_addVariableButton.SetEnabled(_model.IsWritable);
			_addRuleButton.SetEnabled(_model.IsWritable);
			_showAllButton.SetEnabled(_model.IsWritable);
			_sortButton.SetEnabled(_model.IsWritable);
			_optionsButton.SetEnabled(true);
		}

		private void RefreshProperties()
		{
			ClearSpawnedProperties();

			foreach (StylePropertyModel model in _model.PropertyModels)
			{
				if (_model.ShowAll || (!_model.ShowAll && model.IsInStyle))
				{
					StylePropertyVisualElement element = new StylePropertyVisualElement(model);
					element.Init();
					(model.IsVariable ? _variablesParent : _propertiesParent).Add(element);
				}
			}
		}

		private void RegisterButtonActions()
		{
			_undoButton?.RegisterCallback<MouseDownEvent>(_model.UndoButtonClicked);
			_cleanAllButton?.RegisterCallback<MouseDownEvent>(_model.ClearAllButtonClicked);
			_addVariableButton?.RegisterCallback<MouseDownEvent>(_model.AddVariableButtonClicked);
			_addRuleButton?.RegisterCallback<MouseDownEvent>(_model.AddRuleButtonClicked);
			_showAllButton?.RegisterCallback<MouseDownEvent>(_model.ShowAllButtonClicked);
			_sortButton?.RegisterCallback<MouseDownEvent>(_model.SortButtonClicked);
			_optionsButton?.RegisterCallback<MouseDownEvent>(_model.OptionsButtonClicked);
		}

		private void UpdateShowAllStatus()
		{
			EnableInClassList("showAllProperties", _model.ShowAll);
			_showAllButtonText.text = _model.ShowAll ? "Hide All" : "Show All";
		}
	}
}
