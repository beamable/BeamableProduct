using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public class VariableDatabase
	{
		public class PropertyReference
		{
			public readonly int HashKey;
			public readonly string Key;
			public readonly BussPropertyProvider PropertyProvider;
			public readonly BussStyleRule StyleRule;
			public readonly BussStyleSheet StyleSheet;

			public PropertyReference() { }

			public PropertyReference(string key,
									 BussStyleSheet styleSheet,
									 BussStyleRule styleRule,
									 BussPropertyProvider propertyProvider)
			{
				HashKey = Animator.StringToHash(key);
				Key = key;
				StyleSheet = styleSheet;
				StyleRule = styleRule;
				PropertyProvider = propertyProvider;
			}

			public SelectorWeight GetWeight()
			{
				return StyleRule == null ? SelectorWeight.Max : StyleRule.Selector.GetWeight();
			}

			public string GetDisplayString()
			{
				return $"{StyleSheet.name} - {StyleRule.SelectorString} -- {this.PropertyProvider.IsVariable}";
			}
		}

		public enum PropertyValueState
		{
			NoResult,
			SingleResult,
			MultipleResults,
			VariableLoopDetected
		}

		private readonly List<BussStyleSheet> _styleSheets = new List<BussStyleSheet>();
		private readonly HashSet<string> _usedVariableNames = new HashSet<string>();
		private readonly List<PropertyReference> _variables = new List<PropertyReference>();

		private readonly IVariablesProvider _variablesProvider;

		public VariableDatabase(IVariablesProvider variablesProvider)
		{
			_variablesProvider = variablesProvider;
		}

		// public List<PropertyReference> GetVariableData(string key)
		// {
		// 	var hash = Animator.StringToHash(key);
		//
		// 	List<PropertyReference> propertyReferences =
		// 		_variables.FindAll(prop => prop.HashKey == hash);
		//
		// 	if (propertyReferences.Count != 0)
		// 	{
		// 		return propertyReferences;
		// 	}
		//
		// 	var data = new PropertyReference(key, null, null, null);
		// 	_variables.Add(data);
		// 	return new List<PropertyReference> { data };
		// }

		// public List<PropertyReference> GetVariablesOfType(Type baseType)
		// {
		// 	List<PropertyReference> propertyReferences =
		// 		_variables.FindAll(prop => baseType.IsInstanceOfType(prop.PropertyProvider.GetProperty()));
		//
		// 	return propertyReferences;
		// }

		public void ReconsiderAllStyleSheets()
		{
			// _variables.Clear();
			// _styleSheets.Clear();
			//
			// foreach (BussStyleSheet sheet in _variablesProvider.GetStylesheets())
			// {
			// 	AddStyleSheet(sheet);
			// }
		}

		// public void TryGetProperty(BussPropertyProvider basePropertyProvider,
		// 						   BussStyleDescription styleRule,
		// 						   out IBussProperty result,
		// 						   out PropertyReference variablePropertyReference)
		// {
		// 	if (!basePropertyProvider.HasVariableReference)
		// 	{
		// 		variablePropertyReference = new PropertyReference(string.Empty, null, null, null);
		// 		result = basePropertyProvider.GetProperty();
		// 		return;
		// 	}
		//
		// 	FindVariableEndValue((VariableProperty)basePropertyProvider.GetProperty(),
		// 						 styleRule, out result, out variablePropertyReference);
		// }

		private void AddStyleSheet(BussStyleSheet sheet)
		{
			if (sheet == null) return;

			if (_styleSheets.Contains(sheet))
			{
				return;
			}

			foreach (BussStyleRule rule in sheet.Styles)
			{
				foreach (BussPropertyProvider propertyProvider in rule.Properties)
				{
					if (BussStyleSheetUtility.IsValidVariableName(propertyProvider.Key))
					{
						if (propertyProvider.IsVariable)
						{
							_variables.Add(new PropertyReference(propertyProvider.Key, sheet, rule, propertyProvider));
						}
					}
				}
			}

			_styleSheets.Add(sheet);
		}

		/// <summary>
		/// Searches for the variable value without context.
		/// If there is a variable in the same StyleRule, then it is returned.
		/// If there is only one declaration of a variable in variable database, it returns the value of it.
		/// Otherwise returns null.
		/// It can search for end value recursively.
		/// </summary>
		// private PropertyValueState FindVariableEndValue(VariableProperty variableProperty,
		// 												BussStyleDescription styleRule,
		// 												out IBussProperty result,
		// 												out PropertyReference propertyReference)
		// {
		// 	result = null;
		// 	propertyReference = new PropertyReference(string.Empty, null, null, null);
		// 	PropertyValueState state;
		//
		// 	if (_usedVariableNames.Contains(variableProperty.VariableName)) // check if we are not in infinite loop
		// 	{
		// 		_usedVariableNames.Clear();
		// 		return PropertyValueState.VariableLoopDetected;
		// 	}
		//
		// 	_usedVariableNames.Add(variableProperty.VariableName);
		//
		// 	if (styleRule.HasProperty(variableProperty.VariableName))
		// 	{
		// 		state = PropertyValueState.SingleResult;
		// 		result = styleRule.GetProperty(variableProperty.VariableName);
		// 	}
		// 	else
		// 	{
		// 		var variableData = GetVariableData(variableProperty.VariableName);
		//
		// 		if (variableData.Count == 1)
		// 		{
		// 			state = PropertyValueState.SingleResult;
		// 			propertyReference = variableData[0];
		// 			result = propertyReference.PropertyProvider.GetProperty();
		// 		}
		// 		else if (variableData.Count > 1)
		// 		{
		// 			state = PropertyValueState.MultipleResults;
		// 		}
		// 		else
		// 		{
		// 			state = PropertyValueState.NoResult;
		// 		}
		// 	}
		//
		// 	if (result != null && result is VariableProperty nestedVariableProperty)
		// 	{
		// 		state = FindVariableEndValue(nestedVariableProperty, styleRule, out result, out propertyReference);
		// 	}
		//
		// 	_usedVariableNames.Clear();
		//
		// 	return state;
		// }
	}
}
