using System;
using System.Collections.Generic;

namespace Beamable.UI.Buss
{
	public class VariableDatabase
	{
		public class PropertyReference
		{
			public readonly BussPropertyProvider PropertyProvider;
			public readonly BussStyleRule StyleRule;
			public readonly BussStyleSheet StyleSheet;

			public PropertyReference() { }

			public PropertyReference(BussStyleSheet styleSheet,
			                         BussStyleRule styleRule,
			                         BussPropertyProvider propertyProvider)
			{
				StyleSheet = styleSheet;
				StyleRule = styleRule;
				PropertyProvider = propertyProvider;
			}

			public SelectorWeight GetWeight()
			{
				return StyleRule == null ? SelectorWeight.Max : StyleRule.Selector.GetWeight();
			}
		}

		public enum PropertyValueState
		{
			NoResult,
			SingleResult,
			VariableLoopDetected
		}

		private readonly List<BussStyleSheet> _styleSheets = new List<BussStyleSheet>();
		private readonly HashSet<string> _usedVariableNames = new HashSet<string>();
		private readonly Dictionary<string, PropertyReference> _variables = new Dictionary<string, PropertyReference>();

		private readonly IVariablesProvider _variablesProvider;

		public VariableDatabase(IVariablesProvider variablesProvider)
		{
			_variablesProvider = variablesProvider;
		}

		public IBussProperty GetVariable(string key)
		{
			ReconsiderAllStyleSheets();
			var variableData = GetVariableData(key);
			return variableData.PropertyProvider.GetProperty();
		}

		public PropertyReference GetVariableData(string key)
		{
			if (_variables.TryGetValue(key, out var value))
			{
				return value;
			}

			var data = new PropertyReference();
			_variables[key] = data;
			return data;
		}

		public IEnumerable<string> GetVariableNames()
		{
			ReconsiderAllStyleSheets();
			return _variables.Keys;
		}

		public IEnumerable<string> GetVariablesNamesOfType(Type baseType)
		{
			List<string> variablesNames = new List<string>();

			foreach (var pair in _variables)
			{
				if (baseType.IsInstanceOfType(pair.Value.PropertyProvider.GetProperty()))
				{
					variablesNames.Add(pair.Key);
				}
			}

			return variablesNames;
		}

		public bool HasVariable(string key)
		{
			return _variables.TryGetValue(key, out _);
		}

		public void ReconsiderAllStyleSheets()
		{
			_variables.Clear();
			_styleSheets.Clear();

			foreach (BussStyleSheet sheet in _variablesProvider.GetStylesheets())
			{
				AddStyleSheet(sheet);
			}
		}

		public void TryGetProperty(BussPropertyProvider basePropertyProvider,
		                           BussStyleDescription styleRule,
		                           out IBussProperty result,
		                           out PropertyReference variablePropertyReference)
		{
			if (!basePropertyProvider.HasVariableReference)
			{
				variablePropertyReference = new PropertyReference(null, null, null);
				result = basePropertyProvider.GetProperty();
				return;
			}

			FindVariableEndValue((VariableProperty)basePropertyProvider.GetProperty(),
			                     styleRule, out result, out variablePropertyReference);
		}

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
							_variables.Add(propertyProvider.Key, new PropertyReference(sheet, rule, propertyProvider));
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
		private PropertyValueState FindVariableEndValue(VariableProperty variableProperty,
		                                                BussStyleDescription styleRule,
		                                                out IBussProperty result,
		                                                out PropertyReference propertyReference)
		{
			result = null;
			propertyReference = new PropertyReference(null, null, null);
			PropertyValueState state;

			if (_usedVariableNames.Contains(variableProperty.VariableName)) // check if we are not in infinite loop
			{
				_usedVariableNames.Clear();
				return PropertyValueState.VariableLoopDetected;
			}

			_usedVariableNames.Add(variableProperty.VariableName);

			if (styleRule.HasProperty(variableProperty.VariableName))
			{
				state = PropertyValueState.SingleResult;
				result = styleRule.GetProperty(variableProperty.VariableName);
			}
			else
			{
				var variableData = GetVariableData(variableProperty.VariableName);

				if (variableData != null)
				{
					state = PropertyValueState.SingleResult;
					propertyReference = variableData;
					result = propertyReference.PropertyProvider.GetProperty();
				}
				else
				{
					state = PropertyValueState.NoResult;
				}
			}

			if (result != null && result is VariableProperty nestedVariableProperty)
			{
				state = FindVariableEndValue(nestedVariableProperty, styleRule, out result, out propertyReference);
			}

			_usedVariableNames.Clear();

			return state;
		}
	}
}
