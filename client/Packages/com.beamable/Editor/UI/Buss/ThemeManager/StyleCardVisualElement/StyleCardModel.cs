using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class StyleCardModel
	{
		private class PropertyComparer : IComparer<StylePropertyModel>
		{
			public int Compare(StylePropertyModel x, StylePropertyModel y)
			{
				if (x == null || y == null)
				{
					Debug.LogWarning("PropertyComparer:Compare: one of compared elements is null");
					return 0;
				}

				int comparison = (y.IsInStyle).CompareTo(x.IsInStyle);

				if (comparison == 0)
				{
					comparison = string.Compare(x.PropertyProvider.Key, y.PropertyProvider.Key,
												StringComparison.Ordinal);
				}

				return comparison;
			}
		}

		public event Action Change;

		private readonly PropertyComparer _propertyComparer = new PropertyComparer();
		private readonly Action _globalRefresh;
		private readonly ThemeModel.PropertyDisplayFilter _currentDisplayFilter;

		public BussStyleSheet StyleSheet { get; }
		public BussStyleRule StyleRule { get; }
		private Action UndoAction { get; }
		public bool IsSelected { get; }
		private VariableDatabase VariablesDatabase { get; }
		private PropertySourceDatabase PropertiesDatabase { get; }
		private IEnumerable<BussStyleSheet> WritableStyleSheets { get; }
		public bool IsWritable => StyleSheet.IsWritable;
		public bool IsFolded => StyleRule.Folded;
		public bool ShowAll { get; private set; }
		private bool Sorted { get; set; }
		private BussElement SelectedElement { get; }

		public StyleCardModel(BussStyleSheet styleSheet,
							  BussStyleRule styleRule,
							  Action onUndoAction,
							  BussElement selectedElement,
							  bool isSelected,
							  VariableDatabase variablesDatabase,
							  PropertySourceDatabase propertiesDatabase,
							  IEnumerable<BussStyleSheet> writableStyleSheets,
							  Action globalRefresh,
							  ThemeModel.PropertyDisplayFilter currentDisplayFilter)
		{
			StyleSheet = styleSheet;
			StyleRule = styleRule;
			UndoAction = onUndoAction;
			SelectedElement = selectedElement;
			IsSelected = isSelected;
			VariablesDatabase = variablesDatabase;
			PropertiesDatabase = propertiesDatabase;
			WritableStyleSheets = writableStyleSheets;

			_globalRefresh = globalRefresh;
			_currentDisplayFilter = currentDisplayFilter;
		}

		public void AddRuleButtonClicked(MouseDownEvent evt)
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
																   t != typeof(FractionFloatBussProperty)).ToList();

				foreach (Type type in types)
				{
					GUIContent label = new GUIContent(types.Count() > 1 ? key + "/" + type.Name : key);
					context.AddItem(new GUIContent(label), false, () =>
					{
						StyleRule.Properties.Add(
							BussPropertyProvider.Create(key, (IBussProperty)Activator.CreateInstance(type)));
						AssetDatabase.SaveAssets();
						StyleSheet.TriggerChange();
						Change?.Invoke();
					});
				}
			}

			context.ShowAsContext();
		}

		public void AddVariableButtonClicked(MouseDownEvent evt)
		{
			NewVariableWindow window = NewVariableWindow.ShowWindow();

			if (window != null)
			{
				window.Init(StyleRule, (key, property) =>
				{
					if (!StyleRule.TryAddProperty(key, property))
					{
						return;
					}

					AssetDatabase.SaveAssets();
					StyleSheet.TriggerChange();
					Change?.Invoke();
				}, VariablesDatabase);
			}
		}

		public void ClearAllButtonClicked(MouseDownEvent evt)
		{
			BeamablePopupWindow.CloseConfirmationWindow();

			ConfirmationPopupVisualElement confirmationPopup = new ConfirmationPopupVisualElement(
				CLEAR_ALL_PROPERTIES_MESSAGE,
				() =>
				{
					StyleSheet.RemoveAllProperties(StyleRule);
				},
				BeamablePopupWindow.CloseConfirmationWindow
			);

			BeamablePopupWindow.ShowConfirmationUtility(CLEAR_ALL_PROPERTIES_HEADER, confirmationPopup, null);
		}

		public void OptionsButtonClicked(MouseDownEvent evt)
		{
			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in PrepareCommands())
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		public List<GenericMenuCommand> PrepareCommands()
		{
			List<GenericMenuCommand> commands = new List<GenericMenuCommand>();

			if (StyleSheet.IsWritable)
			{
				commands.Add(new GenericMenuCommand(Constants.Features.Buss.MenuItems.DUPLICATE, () =>
				{
					BussStyleSheetUtility.CopySingleStyle(StyleSheet, StyleRule);
					_globalRefresh.Invoke();
				}));
			}

			List<BussStyleSheet> writableStyleSheets = new List<BussStyleSheet>(WritableStyleSheets);
			writableStyleSheets.Remove(StyleSheet);

			if (writableStyleSheets.Count > 0)
			{
				foreach (BussStyleSheet targetStyleSheet in writableStyleSheets)
				{
					commands.Add(new GenericMenuCommand(
									 $"{Constants.Features.Buss.MenuItems.COPY_TO}/{targetStyleSheet.name}",
									 () =>
									 {
										 BussStyleSheetUtility.CopySingleStyle(targetStyleSheet, StyleRule);
										 _globalRefresh.Invoke();
									 }));
				}
			}
			else
			{
				commands.Add(new GenericMenuCommand($"{Constants.Features.Buss.MenuItems.COPY_INTO_NEW_STYLE_SHEET}",
													() =>
													{
														NewStyleSheetWindow window = NewStyleSheetWindow.ShowWindow();
														if (window != null)
														{
															window.Init(new List<BussStyleRule> { StyleRule });
														}
													}));
			}

			if (IsWritable)
			{
				commands.Add(new GenericMenuCommand(Constants.Features.Buss.MenuItems.REMOVE, RemoveStyleClicked));
			}

			return commands;
		}

		public void ShowAllButtonClicked(MouseDownEvent evt)
		{
			ShowAll = !ShowAll;
			Change?.Invoke();
		}

		public void SortButtonClicked(MouseDownEvent evt)
		{
			Sorted = !Sorted;
			Change?.Invoke();
		}

		public void UndoButtonClicked(MouseDownEvent evt)
		{
			UndoAction?.Invoke();
			Change?.Invoke();
		}

		public List<StylePropertyModel> GetProperties(bool sort = true)
		{
			var models = new List<StylePropertyModel>();

			foreach (string key in BussStyle.Keys)
			{
				var propertyProvider = StyleRule.Properties.Find(provider => provider.Key == key) ??
									   BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty());

				var model = new StylePropertyModel(StyleSheet, StyleRule, propertyProvider, VariablesDatabase,
												   PropertiesDatabase.GetTracker(SelectedElement),
												   null, RemovePropertyClicked, _globalRefresh);

				if (!(_currentDisplayFilter == ThemeModel.PropertyDisplayFilter.IgnoreOverridden && model.IsOverriden))
				{
					models.Add(model);
				}
			}

			if (sort)
			{
				models.Sort(_propertyComparer);
			}

			models.AddRange(GetVariables());
			return models;
		}

		private List<StylePropertyModel> GetVariables()
		{
			var variables = new List<StylePropertyModel>();

			foreach (var propertyProvider in StyleRule.Properties)
			{
				if (!propertyProvider.IsVariable)
				{
					continue;
				}

				var model = new StylePropertyModel(StyleSheet, StyleRule, propertyProvider, VariablesDatabase,
												   PropertiesDatabase.GetTracker(SelectedElement), null,
												   RemovePropertyClicked, _globalRefresh);
				variables.Add(model);
			}

			return variables;
		}

		private void RemoveStyleClicked()
		{
			BeamablePopupWindow.CloseConfirmationWindow();

			ConfirmationPopupVisualElement confirmationPopup = new ConfirmationPopupVisualElement(
				DELETE_STYLE_MESSAGE,
				() =>
				{
					BussStyleSheetUtility.RemoveSingleStyle(StyleSheet, StyleRule);
				},
				BeamablePopupWindow.CloseConfirmationWindow
			);

			BeamablePopupWindow.ShowConfirmationUtility(DELETE_STYLE_HEADER, confirmationPopup, null);
		}

		private void RemovePropertyClicked(string propertyKey)
		{
			var propertyModel = GetProperties(false).Find(property => property.PropertyProvider.Key == propertyKey);

			if (propertyModel == null)
			{
				Debug.LogWarning($"StyleCardModel:RemovePropertyCLicked: can't find property with {propertyKey} key");
				return;
			}

			if (propertyModel.InlineStyleOwner != null)
			{
				propertyModel.InlineStyleOwner.InlineStyle.Properties.Remove(propertyModel.PropertyProvider);
			}
			else
			{
				IBussProperty bussProperty = propertyModel.PropertyProvider.GetProperty();
				propertyModel.StyleSheet.RemoveStyleProperty(bussProperty, propertyModel.StyleRule);
			}

			Change?.Invoke();
		}

		public void FoldButtonClicked(MouseDownEvent evt)
		{
			StyleRule.SetFolded(!StyleRule.Folded);
			AssetDatabase.SaveAssets();
			_globalRefresh?.Invoke();
		}
	}
}
