using Beamable.Editor.Common;
using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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
	public class BussStyleCardVisualElement : BeamableVisualElement
	{
		private BussSelectorLabelVisualElement _selectorLabelComponent;
		private VisualElement _selectorLabelParent;
		private VisualElement _variablesParent;
		private VisualElement _propertiesParent;
		private VisualElement _colorBlock;
		private VisualElement _wizardButton;
		private VisualElement _optionsButton;
		private VisualElement _undoButton;
		private VisualElement _cleanAllButton;
		private VisualElement _addVariableButton;
		private VisualElement _addRuleButton;
		private VisualElement _showAllButton;
		private VisualElement _sortButton;
		private TextElement _showAllButtonText;

		private VariableDatabase _variableDatabase;
		private PropertySourceDatabase _propertyDatabase;
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;

		private BussElement _selectedElement;

		private readonly List<BussStylePropertyVisualElement> _properties = new List<BussStylePropertyVisualElement>();
		private Action _onUndoRequest;
		private bool _sorted;
		private bool _showAllMode;
		private IEnumerable<BussStyleSheet> _writableStyleSheets;

		public BussStyleSheet StyleSheet => _styleSheet;
		public BussStyleRule StyleRule => _styleRule;

		public BussStyleCardVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}")
		{ }

		public override void Refresh()
		{
			base.Refresh();

			_properties.Clear();

			_selectorLabelParent = Root.Q<VisualElement>("selectorLabelParent");
			_variablesParent = Root.Q<VisualElement>("variables");
			_propertiesParent = Root.Q<VisualElement>("properties");
			_colorBlock = Root.Q<VisualElement>("colorBlock");

			_wizardButton = Root.Q<VisualElement>("wizardButton");
			if (_wizardButton != null)
			{
				_wizardButton.tooltip = Tooltips.Buss.WIZARD_SYSTEM;
			}

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
		}

		public void RefreshButtons()
		{
			bool enabled = _styleSheet.IsWritable;

			_undoButton.SetEnabled(enabled);
			_cleanAllButton.SetEnabled(enabled);
			_addVariableButton.SetEnabled(enabled);
			_addRuleButton.SetEnabled(enabled);
			_showAllButton.SetEnabled(enabled);
			_sortButton.SetEnabled(enabled);

			_optionsButton.SetEnabled(true);
		}

		public void Setup(BussStyleSheet styleSheet,
						  BussStyleRule styleRule,
						  VariableDatabase variableDatabase,
						  PropertySourceDatabase propertySourceDatabase,
						  Action onUndoRequest,
						  IEnumerable<BussStyleSheet> writableStyleSheets)
		{
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_variableDatabase = variableDatabase;
			_propertyDatabase = propertySourceDatabase;
			_onUndoRequest = onUndoRequest;
			_writableStyleSheets = writableStyleSheets;

			Refresh();
		}

		protected override void OnDestroy()
		{
			ClearButtonActions();
		}

		private void RegisterButtonActions()
		{
			ClearButtonActions();

			_wizardButton?.RegisterCallback<MouseDownEvent>(WizardButtonClicked);
			_undoButton?.RegisterCallback<MouseDownEvent>(UndoButtonClicked);
			_cleanAllButton?.RegisterCallback<MouseDownEvent>(ClearAllButtonClicked);
			_addVariableButton?.RegisterCallback<MouseDownEvent>(AddVariableButtonClicked);
			_addRuleButton?.RegisterCallback<MouseDownEvent>(AddRuleButtonClicked);
			_showAllButton?.RegisterCallback<MouseDownEvent>(ShowAllButtonClicked);
			_sortButton?.RegisterCallback<MouseDownEvent>(SortButtonClicked);
			_optionsButton?.RegisterCallback<MouseDownEvent>(OptionsButtonClicked);
		}

		private void ClearButtonActions()
		{
			_wizardButton?.UnregisterCallback<MouseDownEvent>(WizardButtonClicked);
			_undoButton?.UnregisterCallback<MouseDownEvent>(UndoButtonClicked);
			_cleanAllButton?.UnregisterCallback<MouseDownEvent>(ClearAllButtonClicked);
			_addVariableButton?.UnregisterCallback<MouseDownEvent>(AddVariableButtonClicked);
			_addRuleButton?.UnregisterCallback<MouseDownEvent>(AddRuleButtonClicked);
			_showAllButton?.UnregisterCallback<MouseDownEvent>(ShowAllButtonClicked);
			_sortButton?.UnregisterCallback<MouseDownEvent>(SortButtonClicked);
			_optionsButton?.UnregisterCallback<MouseDownEvent>(OptionsButtonClicked);
		}

		private void OptionsButtonClicked(MouseDownEvent evt)
		{
			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in PrepareCommands())
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		private void AddRuleButtonClicked(MouseDownEvent evt)
		{
			HashSet<string> keys = new HashSet<string>();
			foreach (BussPropertyProvider propertyProvider in StyleRule.Properties)
			{
				keys.Add(propertyProvider.Key);
			}

			IOrderedEnumerable<string> sorted = BussStyle.Keys.OrderBy(k => k);
			GenericMenu context = new GenericMenu();

			foreach (string key in sorted)
			{
				if (keys.Contains(key)) continue;
				Type baseType = BussStyle.GetBaseType(key);
				SerializableValueImplementationHelper.ImplementationData data =
					SerializableValueImplementationHelper.Get(baseType);
				IEnumerable<Type> types = data.subTypes.Where(t => t != null && t.IsClass && !t.IsAbstract &&
																   t != typeof(FractionFloatBussProperty));

				foreach (Type type in types)
				{
					GUIContent label = new GUIContent(types.Count() > 1 ? key + "/" + type.Name : key);
					context.AddItem(new GUIContent(label), false, () =>
					{
						StyleRule.Properties.Add(
							BussPropertyProvider.Create(key, (IBussProperty)Activator.CreateInstance(type)));
						AssetDatabase.SaveAssets();
						_styleSheet.TriggerChange();
					});
				}
			}

			context.ShowAsContext();
		}

		private void AddVariableButtonClicked(MouseDownEvent evt)
		{
			NewVariableWindow window = NewVariableWindow.ShowWindow();

			if (window != null)
			{
				window.Init(_styleRule, (key, property) =>
				{
					if (StyleRule.TryAddProperty(key, property))
					{
						AssetDatabase.SaveAssets();
						_styleSheet.TriggerChange();
					}
				});
			}
		}

		private void ClearAllButtonClicked(MouseDownEvent evt)
		{
			BeamablePopupWindow.CloseConfirmationWindow();

			ConfirmationPopupVisualElement confirmationPopup = new ConfirmationPopupVisualElement(
				CLEAR_ALL_PROPERTIES_MESSAGE,
				() =>
				{
					_styleSheet.RemoveAllProperties(StyleRule);
				},
				BeamablePopupWindow.CloseConfirmationWindow
			);

			BeamablePopupWindow.ShowConfirmationUtility(CLEAR_ALL_PROPERTIES_HEADER, confirmationPopup,
														this.GetEditorWindowWithReflection());
		}

		private void UndoButtonClicked(MouseDownEvent evt)
		{
			_onUndoRequest?.Invoke();
		}

		private void WizardButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("WizardButtonClicked");
		}

		private void ShowAllButtonClicked(MouseDownEvent evt)
		{
			_showAllMode = !_showAllMode;
			UpdateShowAllStatus();
			RefreshProperties();
		}

		private void SortButtonClicked(MouseDownEvent evt)
		{
			_sorted = !_sorted;
			SortProperties();
		}

		private void UpdateShowAllStatus()
		{
			const string showAllProperty = "showAllProperties";
			EnableInClassList(showAllProperty, _showAllMode);
			_showAllButtonText.text = _showAllMode ? "Hide All" : "Show All";
		}

		private void CreateSelectorLabel()
		{
			_selectorLabelComponent?.Destroy();
			_selectorLabelParent.Clear();

			_selectorLabelComponent = new BussSelectorLabelVisualElement();

			_selectorLabelComponent.Setup(StyleRule, _styleSheet, PrepareCommands);
			_selectorLabelParent.Add(_selectorLabelComponent);
		}

		private List<GenericMenuCommand> PrepareCommands()
		{
			List<GenericMenuCommand> commands = new List<GenericMenuCommand>();

			if (_styleSheet.IsWritable)
			{
				commands.Add(new GenericMenuCommand(Features.Buss.MenuItems.DUPLICATE, () =>
				{
					BussStyleSheetUtility.CopySingleStyle(_styleSheet, _styleRule);
				}));
			}

			List<BussStyleSheet> writableStyleSheets = new List<BussStyleSheet>(_writableStyleSheets);
			writableStyleSheets.Remove(_styleSheet);

			if (writableStyleSheets.Count > 0)
			{
				foreach (BussStyleSheet targetStyleSheet in writableStyleSheets)
				{
					commands.Add(new GenericMenuCommand($"{Features.Buss.MenuItems.COPY_TO}/{targetStyleSheet.name}",
														() =>
														{
															BussStyleSheetUtility.CopySingleStyle(
																targetStyleSheet, _styleRule);
														}));
				}
			}
			else
			{
				commands.Add(new GenericMenuCommand($"{Features.Buss.MenuItems.COPY_INTO_NEW_STYLE_SHEET}", () =>
				{
					NewStyleSheetWindow window = NewStyleSheetWindow.ShowWindow();
					if (window != null)
					{
						window.Init(new List<BussStyleRule> { _styleRule });
					}
				}));
			}

			if (_styleSheet.IsWritable)
			{
				commands.Add(new GenericMenuCommand(Features.Buss.MenuItems.REMOVE, RemoveStyleClicked));
			}

			return commands;
		}

		private void RemoveStyleClicked()
		{
			BeamablePopupWindow.CloseConfirmationWindow();

			ConfirmationPopupVisualElement confirmationPopup = new ConfirmationPopupVisualElement(
				DELETE_STYLE_MESSAGE,
				() =>
				{
					BussStyleSheetUtility.RemoveSingleStyle(_styleSheet, _styleRule);
				},
				BeamablePopupWindow.CloseConfirmationWindow
			);

			BeamablePopupWindow.ShowConfirmationUtility(DELETE_STYLE_HEADER, confirmationPopup,
														this.GetEditorWindowWithReflection());
		}

		public void RefreshProperties()
		{
			foreach (BussStylePropertyVisualElement element in _properties.ToArray())
			{
				bool remove;

				if (element.PropertyIsInStyle)
				{
					remove = !_styleRule.Properties.Contains(element.PropertyProvider);
				}
				else
				{
					remove = !_showAllMode ||
							 _styleRule.Properties.Any(p => p.Key == element.PropertyKey);
				}

				if (remove)
				{
					element.RemoveFromHierarchy();
					element.Destroy();
					_properties.Remove(element);
				}
			}

			foreach (BussPropertyProvider property in _styleRule.Properties)
			{
				BussStylePropertyVisualElement existingProperty =
					_properties.FirstOrDefault(p => p.PropertyProvider == property);
				if (existingProperty != null)
				{
					existingProperty.Refresh();
					continue;
				}

				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(_styleSheet, _styleRule, property, _variableDatabase,
							  _propertyDatabase.GetTracker(_selectedElement));
				(property.IsVariable ? _variablesParent : _propertiesParent).Add(element);
				_properties.Add(element);
			}

			if (_showAllMode)
			{
				IOrderedEnumerable<string> restPropertyKeys =
					BussStyle.Keys.Where(s => StyleRule.Properties.All(provider => provider.Key != s)).OrderBy(k => k);

				foreach (string key in restPropertyKeys)
				{
					BussStylePropertyVisualElement existingProperty =
						_properties.FirstOrDefault(p => p.PropertyProvider.Key == key);
					if (existingProperty != null)
					{
						existingProperty.Refresh();
						continue;
					}

					BussPropertyProvider propertyProvider =
						BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty());
					BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
					element.Setup(_styleSheet, StyleRule, propertyProvider, _variableDatabase,
								  _propertyDatabase.GetTracker(_selectedElement));
					_propertiesParent.Add(element);
					_properties.Add(element);
				}
			}

			SortProperties();
		}

		private void SortProperties()
		{
			if (!_sorted)
			{
				_propertiesParent.Sort((a, b) =>
				{
					if (!(a is BussStylePropertyVisualElement p1) || !(b is BussStylePropertyVisualElement p2))
					{
						return 0;
					}

					if (p1.PropertyIsInStyle == p2.PropertyIsInStyle)
					{
						if (p1.PropertyIsInStyle)
						{
							List<BussPropertyProvider> properties = _styleRule.Properties;
							return properties.IndexOf(p1.PropertyProvider) - properties.IndexOf(p2.PropertyProvider);
						}

						string[] keys = BussStyle.Keys.ToArray();
						return Array.IndexOf(keys, p1.PropertyProvider.Key) -
							   Array.IndexOf(keys, p2.PropertyProvider.Key);
					}

					return p2.PropertyIsInStyle ? 1 : -1;
				});
			}
			else
			{
				_propertiesParent.Sort((a, b) =>
				{
					if (!(a is BussStylePropertyVisualElement p1) || !(b is BussStylePropertyVisualElement p2))
					{
						return 0;
					}

					if (p1.PropertyIsInStyle == p2.PropertyIsInStyle)
					{
						return String.Compare(p1.PropertyKey, p2.PropertyKey, StringComparison.Ordinal);
					}

					return p2.PropertyIsInStyle ? 1 : -1;
				});
			}
		}

		public void RefreshPropertyByReference(VariableDatabase.PropertyReference reference)
		{
			BussStylePropertyVisualElement property =
				_properties.FirstOrDefault(p => p.PropertyProvider == reference.propertyProvider);
			if (property != null)
			{
				property.Refresh();
			}
		}

		public void OnBussElementSelected(BussElement element)
		{
			_selectedElement = element;
			PropertySourceTracker tracker = _propertyDatabase.GetTracker(_selectedElement);
			foreach (BussStylePropertyVisualElement propertyVisualElement in _properties)
			{
				propertyVisualElement.SetPropertySourceTracker(tracker);
			}

			if (_colorBlock == null) return;

			bool active = false;
			if (element != null && StyleRule.Selector != null)
			{
				active = StyleRule.Selector.CheckMatch(element);
			}

			_colorBlock.EnableInClassList("active", active);
		}

		public void RefreshWritableStyleSheets(IEnumerable<BussStyleSheet> writableStyleSheets)
		{
			_writableStyleSheets = writableStyleSheets;
		}
	}
}
