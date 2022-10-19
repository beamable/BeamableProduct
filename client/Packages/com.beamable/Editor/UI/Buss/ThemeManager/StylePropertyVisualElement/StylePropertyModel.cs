using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Validation;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class StylePropertyModel
	{
		private readonly Action<string> _removePropertyAction;
		private readonly Action _globalRefresh;

		public BussStyleSheet StyleSheet { get; }
		public BussStyleRule StyleRule { get; }
		public BussPropertyProvider PropertyProvider { get; }
		private VariableDatabase VariablesDatabase { get; }
		private PropertySourceTracker PropertySourceTracker { get; }
		public BussElement InlineStyleOwner { get; }
		public string Tooltip { get; }
		public int VariableDropdownOptionIndex => GetOptionIndex();
		public List<string> DropdownOptions => GetDropdownOptions();

		public bool IsVariable => PropertyProvider.IsVariable;
		public bool IsInStyle => IsInline || (StyleRule != null && StyleRule.Properties.Contains(PropertyProvider));
		public bool IsWritable => IsInline || (StyleSheet != null && StyleSheet.IsWritable);
		private bool IsInline => InlineStyleOwner != null;
		public bool HasVariableConnected => PropertyProvider.HasVariableReference;

		public bool IsOverriden =>
			PropertySourceTracker != null && PropertySourceTracker != null &
			PropertyProvider != PropertySourceTracker.GetUsedPropertyProvider(PropertyProvider.Key);

		public StylePropertyModel(BussStyleSheet styleSheet,
								  BussStyleRule styleRule,
								  BussPropertyProvider propertyProvider,
								  VariableDatabase variablesDatabase,
								  PropertySourceTracker propertySourceTracker,
								  BussElement inlineStyleOwner,
								  Action<string> removePropertyAction,
								  Action globalRefresh)
		{
			_removePropertyAction = removePropertyAction;
			_globalRefresh = globalRefresh;
			StyleSheet = styleSheet;
			StyleRule = styleRule;
			PropertyProvider = propertyProvider;
			VariablesDatabase = variablesDatabase;
			PropertySourceTracker = propertySourceTracker;
			InlineStyleOwner = inlineStyleOwner;

			if (IsOverriden && IsInStyle && PropertySourceTracker != null)
			{
				VariableDatabase.PropertyReference reference =
					PropertySourceTracker.GetUsedPropertyReference(PropertyProvider.Key);

				Tooltip = reference.StyleRule != null
					? $"Property is overriden by {reference.StyleRule.SelectorString} rule from {reference.StyleSheet.name} stylesheet"
					: "Property is overriden by inline style";
			}
			else
			{
				Tooltip = String.Empty;
			}
		}

		public void GetResult(out IBussProperty bussProperty, out VariableDatabase.PropertyReference propertyReference)
		{
			VariablesDatabase.TryGetProperty(PropertyProvider, StyleRule, out IBussProperty property,
											 out VariableDatabase.PropertyReference variableSource);

			bussProperty = property;
			propertyReference = variableSource;
		}

		public void LabelClicked(MouseDownEvent evt)
		{
			if (StyleSheet != null && !StyleSheet.IsWritable)
			{
				return;
			}

			List<GenericMenuCommand> commands = new List<GenericMenuCommand>
			{
				new GenericMenuCommand(Constants.Features.Buss.MenuItems.REMOVE, () =>
				{
					_removePropertyAction?.Invoke(PropertyProvider.Key);
				})
			};

			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in commands)
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		public void OnButtonClick(MouseDownEvent mouseDownEvent)
		{
			if (StyleRule.TryGetCachedProperty(PropertyProvider.Key, out var property))
			{
				PropertyProvider.SetProperty(property);
				StyleRule.RemoveCachedProperty(PropertyProvider.Key);
			}
			else
			{
				StyleRule.CacheProperty(PropertyProvider.Key, PropertyProvider.GetProperty());
				PropertyProvider.SetProperty(new VariableProperty());
			}

			if (StyleSheet != null)
			{
				StyleSheet.TriggerChange();
			}

			AssetDatabase.SaveAssets();
			_globalRefresh?.Invoke();
		}

		public void OnVariableSelected(int index)
		{
			if (!HasVariableConnected)
			{
				return;
			}

			var option = DropdownOptions[index];

			((VariableProperty)PropertyProvider.GetProperty()).VariableName =
				option == Constants.Features.Buss.MenuItems.NONE ? "" : option;

			if (StyleSheet != null)
			{
				StyleSheet.TriggerChange();
			}

			AssetDatabase.SaveAssets();
			_globalRefresh?.Invoke();
		}

		private List<string> GetDropdownOptions()
		{
			var baseType = BussStyle.GetBaseType(PropertyProvider.Key);
			var options = new List<string>();

			options.Clear();
			options.Add(Constants.Features.Buss.MenuItems.NONE);

			List<VariableDatabase.PropertyReference> references = VariablesDatabase.GetVariablesOfType(baseType);

			foreach (VariableDatabase.PropertyReference propertyReference in references)
			{
				if (!options.Contains(propertyReference.Key))
				{
					options.Add(propertyReference.Key);
				}
			}

			return options;
		}

		private int GetOptionIndex()
		{
			List<string> options = GetDropdownOptions();

			string variableName = string.Empty;
			if (HasVariableConnected)
			{
				variableName = ((VariableProperty)PropertyProvider.GetProperty()).VariableName;
			}

			var value = options.FindIndex(option => option.Equals(variableName));
			value = Mathf.Clamp(value, 0, options.Count - 1);
			return value;
		}

		public void OnPropertyChanged(IBussProperty property)
		{
			if (StyleRule != null)
			{
				if (!StyleRule.HasProperty(PropertyProvider.Key))
				{
					StyleRule.TryAddProperty(PropertyProvider.Key, property);
				}
				else
				{
					StyleRule.GetPropertyProvider(PropertyProvider.Key).SetProperty(property);
				}
			}

			if (StyleSheet != null)
			{
#if UNITY_EDITOR
				EditorUtility.SetDirty(StyleSheet);
#endif
				StyleSheet.TriggerChange();
			}

			AssetDatabase.SaveAssets();

			if (InlineStyleOwner != null)
			{
				InlineStyleOwner.Reenable();
			}
		}
	}
}
