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

namespace Beamable.Editor.UI.Components
{
	public class BussStyleCardVisualElement : BeamableVisualElement
	{
		public event Action EnterEditMode;

		private BussSelectorLabelVisualElement _selectorLabelComponent;
		private VisualElement _selectorLabelParent;
		private VisualElement _variables;
		private VisualElement _propertiesParent;
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

		private VariableDatabase _variableDatabase;
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;
		private BussElementHierarchyVisualElement _navigationWindow;

		private Action _onUndoRequest;
		private readonly List<BussStylePropertyVisualElement> _properties = new List<BussStylePropertyVisualElement>();
		private BussThemeManager _themeManager;

		public BussStyleRule StyleRule => _styleRule;

		public BussStyleCardVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}")
		{ }

		public override void Refresh()
		{
			base.Refresh();

			_properties.Clear();

			_selectorLabelParent = Root.Q<VisualElement>("selectorLabelParent");
			_variables = Root.Q<VisualElement>("variables");
			_propertiesParent = Root.Q<VisualElement>("properties");
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

			_removeButton.SetHidden(!StyleRule.EditMode);

			RegisterButtonActions();
			CreateSelectorLabel();
			RefreshProperties();

			UpdateShowAllStatus();

			RefreshButtons();
		}

		public void RefreshButtons()
		{
			bool enabled = !_styleSheet.IsReadOnly;

			_editButton.SetEnabled(enabled);
			_undoButton.SetEnabled(enabled);
			_cleanAllButton.SetEnabled(enabled);
			_addVariableButton.SetEnabled(enabled);
			_addRuleButton.SetEnabled(enabled);
			_showAllButton.SetEnabled(enabled);
		}

		public void Setup(BussThemeManager themeManager,
						  BussStyleSheet styleSheet,
						  BussStyleRule styleRule,
						  VariableDatabase variableDatabase,
						  BussElementHierarchyVisualElement navigationWindow,
						  Action onUndoRequest)
		{
			_themeManager = themeManager;
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_variableDatabase = variableDatabase;
			_navigationWindow = navigationWindow;
			_onUndoRequest = onUndoRequest;

			Refresh();
		}

		protected override void OnDestroy()
		{
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
			_cleanAllButton?.RegisterCallback<MouseDownEvent>(ClearAllButtonClicked);
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
			_cleanAllButton?.UnregisterCallback<MouseDownEvent>(ClearAllButtonClicked);
			_addVariableButton?.UnregisterCallback<MouseDownEvent>(AddVariableButtonClicked);
			_addRuleButton?.UnregisterCallback<MouseDownEvent>(AddRuleButtonClicked);
			_showAllButton?.UnregisterCallback<MouseDownEvent>(ShowAllButtonClicked);
		}

		private void RemoveButtonClicked(MouseDownEvent evt)
		{
			_themeManager.CloseConfirmationPopup();

			ConfirmationPopupVisualElement confirmationPopup = new ConfirmationPopupVisualElement(
				BussConstants.DeleteStyleMessage,
				() =>
				{
					_styleSheet.RemoveStyle(StyleRule);
					AssetDatabase.SaveAssets();
				},
				_themeManager.CloseConfirmationPopup
			);

			BeamablePopupWindow popupWindow = BeamablePopupWindow.ShowConfirmationUtility(
				BussConstants.DeleteStyleHeader,
				confirmationPopup, _themeManager);

			_themeManager.SetConfirmationPopup(popupWindow);
		}

		private void AddRuleButtonClicked(MouseDownEvent evt)
		{
			var keys = new HashSet<string>();
			foreach (var propertyProvider in StyleRule.Properties)
			{
				keys.Add(propertyProvider.Key);
			}

			var context = new GenericMenu();

			foreach (string key in BussStyle.Keys)
			{
				if (keys.Contains(key)) continue;
				var baseType = BussStyle.GetBaseType(key);
				var data = SerializableValueImplementationHelper.Get(baseType);
				var types = data.subTypes.Where(t => t != null && t.IsClass && !t.IsAbstract && t != typeof(FractionFloatBussProperty));
				foreach (Type type in types)
				{
					var label = new GUIContent(types.Count() > 1 ? key + "/" + type.Name : key);
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
			var window = NewVariableWindow.ShowWindow();
			window?.Init(_styleRule, (key, property) =>
			{
				StyleRule.TryAddProperty(key, property, out _);
				AssetDatabase.SaveAssets();
				_styleSheet.TriggerChange();
			});
		}

		private void ClearAllButtonClicked(MouseDownEvent evt)
		{
			_themeManager.CloseConfirmationPopup();

			ConfirmationPopupVisualElement confirmationPopup = new ConfirmationPopupVisualElement(
				BussConstants.ClearAllPropertiesMessage,
				() =>
				{
					_styleSheet.RemoveAllProperties(StyleRule);
				},
				_themeManager.CloseConfirmationPopup
			);

			BeamablePopupWindow popupWindow = BeamablePopupWindow.ShowConfirmationUtility(
				BussConstants.ClearAllPropertiesHeader,
				confirmationPopup, _themeManager);

			_themeManager.SetConfirmationPopup(popupWindow);
		}

		private void UndoButtonClicked(MouseDownEvent evt)
		{
			_onUndoRequest?.Invoke();
		}

		private void WizardButtonClicked(MouseDownEvent evt)
		{
			Debug.Log("WizardButtonClicked");
		}

		private void EditButtonClicked(MouseDownEvent evt)
		{
			SetEditMode(!StyleRule.EditMode);
		}

		public void SetEditMode(bool value)
		{
			StyleRule.EditMode = value;

			if (!StyleRule.EditMode)
			{
				_themeManager.CloseConfirmationPopup();
			}

			Refresh();
			if (value)
			{
				EnterEditMode?.Invoke();
			}
		}

		private void ShowAllButtonClicked(MouseDownEvent evt)
		{
			StyleRule.ShowAllMode = !StyleRule.ShowAllMode;
			UpdateShowAllStatus();
			RefreshProperties();
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
			_selectorLabelComponent.Setup(StyleRule, _styleSheet);
			_selectorLabelParent.Add(_selectorLabelComponent);

			_selectorLabelComponent.OnChangeSubmit += () => SetEditMode(false);
		}

		public void RefreshProperties()
		{
			foreach (BussStylePropertyVisualElement element in _properties.ToArray())
			{
				bool remove = false;
				if (element.PropertyIsInStyle)
				{
					remove = !_styleRule.Properties.Contains(element.PropertyProvider);
				}
				else
				{
					remove = !_styleRule.ShowAllMode ||
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
				var existingProperty = _properties.FirstOrDefault(p => p.PropertyProvider == property);
				if (existingProperty != null)
				{
					existingProperty.Refresh();
					continue;
				}

				var element = new BussStylePropertyVisualElement();
				element.Setup(_styleSheet, _styleRule, property, _variableDatabase);
				(property.IsVariable ? _variables : _propertiesParent).Add(element);
				_properties.Add(element);
			}

			if (_styleRule.ShowAllMode)
			{
				var restPropertyKeys =
					BussStyle.Keys.Where(s => StyleRule.Properties.All(provider => provider.Key != s));
				foreach (var key in restPropertyKeys)
				{
					var existingProperty = _properties.FirstOrDefault(p => p.PropertyProvider.Key == key);
					if (existingProperty != null)
					{
						existingProperty.Refresh();
						continue;
					}

					var propertyProvider =
						BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty());
					BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
					element.Setup(_styleSheet, StyleRule, propertyProvider, _variableDatabase);
					_propertiesParent.Add(element);
					_properties.Add(element);
				}
			}

			_propertiesParent.Sort((a, b) =>
			{
				if (!(a is BussStylePropertyVisualElement p1) || !(b is BussStylePropertyVisualElement p2)) return 0;
				var value = 0;
				if (p1.PropertyIsInStyle) value--;
				if (p2.PropertyIsInStyle) value++;
				if (value == 0)
				{
					if (p1.PropertyIsInStyle)
					{
						var properties = _styleRule.Properties;
						return properties.IndexOf(p1.PropertyProvider) - properties.IndexOf(p2.PropertyProvider);
					}
					else
					{
						var keys = BussStyle.Keys.ToArray();
						return Array.IndexOf(keys, p1.PropertyProvider.Key) -
							   Array.IndexOf(keys, p2.PropertyProvider.Key);
					}
				}
				return value;
			});
		}

		public void RefreshPropertyByReference(VariableDatabase.PropertyReference reference)
		{
			var property = _properties.FirstOrDefault(p => p.PropertyProvider == reference.propertyProvider);
			if (property != null)
			{
				property.Refresh();
			}
		}

		// TODO: change this, card should be setup/refreshed by it's parent
		private void OnSelectionChanged(GameObject gameObject)
		{
			if (_colorBlock == null || gameObject == null) return;

			bool active = false;
			var bussElement = gameObject.GetComponent<BussElement>();
			if (bussElement != null && StyleRule.Selector != null)
			{
				active = StyleRule.Selector.CheckMatch(bussElement);
			}

			_colorBlock.EnableInClassList("active", active);
		}
	}
}
