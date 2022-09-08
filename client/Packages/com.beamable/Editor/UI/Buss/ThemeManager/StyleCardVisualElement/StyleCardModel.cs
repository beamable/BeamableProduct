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
				int comparison = (y != null && y.IsInStyle).CompareTo(x != null && x.IsInStyle);

				if (comparison == 0)
				{
					comparison = String.Compare(x.PropertyProvider.Key, y.PropertyProvider.Key,
					                            StringComparison.Ordinal);
				}

				return comparison;
			}
		}

		public event Action Change;

		private PropertyComparer _propertyComparer = new PropertyComparer();

		public BussStyleSheet StyleSheet { get; }
		public BussStyleRule StyleRule { get; }
		private Action UndoAction { get; }
		public bool IsSelected { get; }
		public VariableDatabase VariablesDatabase { get; }
		public PropertySourceDatabase PropertiesDatabase { get; }
		private IEnumerable<BussStyleSheet> WritableStyleSheets { get; }
		public bool IsWritable => StyleSheet.IsWritable;
		public bool ShowAll { get; private set; }
		public bool Sorted { get; set; }
		public BussElement SelectedElement { get; }
		public List<StylePropertyModel> PropertyModels => GetModels();

		public StyleCardModel(BussStyleSheet styleSheet,
		                      BussStyleRule styleRule,
		                      Action onUndoAction,
		                      BussElement selectedElement,
		                      bool isSelected,
		                      VariableDatabase variablesDatabase,
		                      PropertySourceDatabase propertiesDatabase,
		                      IEnumerable<BussStyleSheet> writableStyleSheets)
		{
			StyleSheet = styleSheet;
			StyleRule = styleRule;
			UndoAction = onUndoAction;
			SelectedElement = selectedElement;
			IsSelected = isSelected;
			VariablesDatabase = variablesDatabase;
			PropertiesDatabase = propertiesDatabase;
			WritableStyleSheets = writableStyleSheets;
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
				                                                   t != typeof(FractionFloatBussProperty));

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
					if (StyleRule.TryAddProperty(key, property))
					{
						AssetDatabase.SaveAssets();
						StyleSheet.TriggerChange();
						Change?.Invoke();
					}
				});
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

			// BeamablePopupWindow.ShowConfirmationUtility(CLEAR_ALL_PROPERTIES_HEADER, confirmationPopup,
			//                                             this.GetEditorWindowWithReflection());

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
							             BussStyleSheetUtility.CopySingleStyle(
								             targetStyleSheet, StyleRule);
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
						                                    window.Init(new List<BussStyleRule> {StyleRule});
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

		private List<StylePropertyModel> GetModels()
		{
			var models = new List<StylePropertyModel>();

			foreach (string key in BussStyle.Keys)
			{
				var propertyProvider = StyleRule.Properties.Find(provider => provider.Key == key) ??
				                       BussPropertyProvider.Create(key, BussStyle.GetDefaultValue(key).CopyProperty());

				StylePropertyModel model = new StylePropertyModel(StyleSheet, StyleRule, propertyProvider,
				                                                  VariablesDatabase,
				                                                  PropertiesDatabase.GetTracker(SelectedElement),
				                                                  null);

				models.Add(model);
			}

			models.Sort(_propertyComparer);

			return models;
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
	}
}
