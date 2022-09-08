using Beamable.UI.Buss;
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
		private readonly List<StylePropertyVisualElement> _spawnedProperties = new List<StylePropertyVisualElement>();
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

		private void OnChange()
		{
			RefreshProperties();
			UpdateShowAllStatus();
			RefreshButtons();
		}

		// public void RefreshPropertyByReference(VariableDatabase.PropertyReference reference)
		// {
		// 	BussStylePropertyVisualElement property =
		// 		_properties.FirstOrDefault(p => p.PropertyProvider == reference.PropertyProvider);
		// 	if (property != null)
		// 	{
		// 		property.Refresh();
		// 	}

		// }

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

		private void CreateSelectorLabel()
		{
			_selectorLabelComponent?.Destroy();
			_selectorLabelParent.Clear();

			_selectorLabelComponent = new BussSelectorLabelVisualElement();

			_selectorLabelComponent.Setup(_model.StyleRule, _model.StyleSheet, _model.PrepareCommands);
			_selectorLabelParent.Add(_selectorLabelComponent);
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

			foreach (BussPropertyProvider property in _model.StyleRule.Properties)
			{
				StylePropertyModel model = new StylePropertyModel(_model.StyleSheet, _model.StyleRule, property, _model.VariablesDatabase,
				                                                  _model.PropertiesDatabase.GetTracker(_model.SelectedElement), null);
				StylePropertyVisualElement element = new StylePropertyVisualElement(model);
				element.Init();
				(property.IsVariable ? _variablesParent : _propertiesParent).Add(element);
				_spawnedProperties.Add(element);
			}

			if (_model.ShowAll)
			{
				IOrderedEnumerable<string> restPropertyKeys =
					BussStyle.Keys.Where(s => _model.StyleRule.Properties.All(provider => provider.Key != s))
					         .OrderBy(k => k);

				foreach (string key in restPropertyKeys)
				{
					StylePropertyVisualElement existingProperty =
						_spawnedProperties.FirstOrDefault(p => p.PropertyProvider.Key == key);
					if (existingProperty != null)
					{
						existingProperty.Refresh();
						continue;
					}

					BussPropertyProvider propertyProvider =
						BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty());
					
					StylePropertyModel model = new StylePropertyModel(_model.StyleSheet, _model.StyleRule, propertyProvider, _model.VariablesDatabase,
					                                                  _model.PropertiesDatabase.GetTracker(_model.SelectedElement), null);
					
					StylePropertyVisualElement element = new StylePropertyVisualElement(model);
					element.Init();
					// element.Setup(_model.StyleSheet, _model.StyleRule, propertyProvider, _model.VariablesDatabase,
					//               _model.PropertiesDatabase.GetTracker(_model.SelectedElement));
					_propertiesParent.Add(element);
					_spawnedProperties.Add(element);
				}
			}

			SortProperties();
		}

		private void ClearSpawnedProperties()
		{
			foreach (var propertyVisualElement in _spawnedProperties)
			{
				propertyVisualElement.Destroy();
			}

			_spawnedProperties.Clear();
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

		private void SortProperties()
		{
			// if (!_sorted)
			// {
			// 	_propertiesParent.Sort((a, b) =>
			// 	{
			// 		if (!(a is BussStylePropertyVisualElement p1) || !(b is BussStylePropertyVisualElement p2))
			// 		{
			// 			return 0;
			// 		}
			//
			// 		if (p1.PropertyIsInStyle == p2.PropertyIsInStyle)
			// 		{
			// 			if (p1.PropertyIsInStyle)
			// 			{
			// 				List<BussPropertyProvider> properties = _styleRule.Properties;
			// 				return properties.IndexOf(p1.PropertyProvider) - properties.IndexOf(p2.PropertyProvider);
			// 			}
			//
			// 			string[] keys = BussStyle.Keys.ToArray();
			// 			return Array.IndexOf(keys, p1.PropertyProvider.Key) -
			// 			       Array.IndexOf(keys, p2.PropertyProvider.Key);
			// 		}
			//
			// 		return p2.PropertyIsInStyle ? 1 : -1;
			// 	});
			// }
			// else
			// {
			// 	_propertiesParent.Sort((a, b) =>
			// 	{
			// 		if (!(a is BussStylePropertyVisualElement p1) || !(b is BussStylePropertyVisualElement p2))
			// 		{
			// 			return 0;
			// 		}
			//
			// 		if (p1.PropertyIsInStyle == p2.PropertyIsInStyle)
			// 		{
			// 			return String.Compare(p1.PropertyKey, p2.PropertyKey, StringComparison.Ordinal);
			// 		}
			//
			// 		return p2.PropertyIsInStyle ? 1 : -1;
			// 	});
			// }
		}

		private void UpdateShowAllStatus()
		{
			EnableInClassList("showAllProperties", _model.ShowAll);
			_showAllButtonText.text = _model.ShowAll ? "Hide All" : "Show All";
		}
	}
}
