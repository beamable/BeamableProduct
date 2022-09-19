using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public string Tooltip { get; set; }
		public int VariableDropdownOptionIndex => GetOptionIndex();
		public List<string> DropdownOptions => GetDropdownOptions();

		public bool IsVariable => PropertyProvider.IsVariable;
		public bool IsInStyle => IsInline || (StyleRule != null && StyleRule.Properties.Contains(PropertyProvider));
		public bool IsWritable => IsInline || (StyleSheet != null && StyleSheet.IsWritable);
		private bool IsInline => InlineStyleOwner != null;
		public bool HasVariableConnected => PropertyProvider.HasVariableReference;

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
		}

		public VariableDatabase.PropertyValueState GetResult(out PropertySourceTracker propertySourceTracker,
		                                                     out IBussProperty bussProperty,
		                                                     out VariableDatabase.PropertyReference propertyReference)
		{
			PropertySourceTracker context = null;
			
			if (PropertySourceTracker != null && PropertySourceTracker.Element != null)
			{
				if (StyleRule?.Selector?.CheckMatch(PropertySourceTracker.Element) ?? false)
				{
					context = PropertySourceTracker;
				}
			}

			VariableDatabase.PropertyValueState result =
				StylePropertyVisualElementUtility.TryGetProperty(PropertyProvider, StyleRule, VariablesDatabase,
				                                                 context, out IBussProperty property,
				                                                 out VariableDatabase.PropertyReference
					                                                 variableSource);

			propertySourceTracker = context;
			bussProperty = property;
			propertyReference = variableSource;

			return result;
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

		public void OnButtonClick()
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
			options.AddRange(VariablesDatabase.GetVariableNames()
			                                  .Where(key => VariablesDatabase.GetVariableData(key)
			                                                                 .HasTypeDeclared(baseType)));

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
	}
}
