using Beamable.Editor.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.UI.Buss
{
	public class VariableDatabase
	{
		public class VariableData
		{
			public readonly List<PropertyReference> Declarations = new List<PropertyReference>();
			public readonly List<PropertyReference> Usages = new List<PropertyReference>();

			public IEnumerable<PropertyReference> GetDeclarationsFrom(BussStyleSheet sheet)
			{
				return Declarations.Where(pr => pr.StyleSheet == sheet);
			}

			public PropertyReference GetDeclarationsFrom(BussStyleRule rule)
			{
				return Declarations.FirstOrDefault(pr => pr.StyleRule == rule);
			}

			public PropertyReference GetUsageFrom(BussStyleRule rule)
			{
				return Usages.FirstOrDefault(pr => pr.StyleRule == rule);
			}

			public IEnumerable<PropertyReference> GetUsagesFrom(BussStyleSheet sheet)
			{
				return Usages.Where(pr => pr.StyleSheet == sheet);
			}

			public bool HasTypeDeclared(Type type)
			{
				return Declarations.Any(pr => type.IsInstanceOfType(pr.PropertyProvider.GetProperty()));
			}
		}

		public readonly struct PropertyReference
		{
			public readonly BussStyleSheet StyleSheet;
			public readonly BussStyleRule StyleRule;
			public readonly BussPropertyProvider PropertyProvider;

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
			MultipleResults,
			VariableLoopDetected
		}

		private readonly ThemeManagerModel _model;
		private readonly HashSet<string> _usedVariableNames = new HashSet<string>();
		private readonly List<BussStyleSheet> _styleSheets = new List<BussStyleSheet>();
		private readonly Dictionary<string, VariableData> _variables = new Dictionary<string, VariableData>();

		public VariableDatabase(ThemeManagerModel model)
		{
			_model = model;
		}

		public VariableData GetVariableData(string key)
		{
			if (_variables.TryGetValue(key, out var value))
			{
				return value;
			}

			var data = new VariableData();
			_variables[key] = data;
			return data;
		}

		public IEnumerable<string> GetVariableNames()
		{
			ReconsiderAllStyleSheets();
			return _variables.Keys;
		}

		public void ReconsiderAllStyleSheets()
		{
			_variables.Clear();
			_styleSheets.Clear();

			foreach (BussStyleSheet sheet in _model.StyleSheets)
			{
				AddStyleSheet(sheet);
			}
		}

		public PropertyValueState TryGetProperty(BussPropertyProvider basePropertyProvider,
		                                         BussStyleDescription styleRule,
		                                         VariableDatabase variableDatabase,
		                                         PropertySourceTracker context,
		                                         out IBussProperty result,
		                                         out PropertyReference variablePropertyReference)
		{
			if (!basePropertyProvider.HasVariableReference)
			{
				variablePropertyReference = new PropertyReference(null, null, null);
				result = basePropertyProvider.GetProperty();
				return PropertyValueState.SingleResult;
			}

			if (context != null)
			{
				return FindVariableEndValueWithContext((VariableProperty)basePropertyProvider.GetProperty(),
				                                       context, BussStyle.GetBaseType(basePropertyProvider.Key),
				                                       out result, out variablePropertyReference);
			}

			return FindVariableEndValue((VariableProperty)basePropertyProvider.GetProperty(),
			                            styleRule, variableDatabase, out result, out variablePropertyReference);
		}

		private void AddStyleSheet(BussStyleSheet sheet)
		{
			if (sheet == null) return;

			if (_styleSheets.Contains(sheet))
			{
				RemoveStyleSheet(sheet);
			}

			foreach (BussStyleRule rule in sheet.Styles)
			{
				foreach (BussPropertyProvider propertyProvider in rule.Properties)
				{
					var property = propertyProvider.GetProperty();
					if (BussStyleSheetUtility.IsValidVariableName(propertyProvider.Key))
					{
						GetVariableData(propertyProvider.Key).Declarations
						                                     .Add(new PropertyReference(sheet, rule, propertyProvider));
					}

					if (property is VariableProperty variableProperty &&
					    BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
					{
						GetVariableData(variableProperty.VariableName).Usages
						                                              .Add(
							                                              new PropertyReference(
								                                              sheet, rule, propertyProvider));
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
		                                                VariableDatabase variableDatabase,
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
				var variableData = variableDatabase.GetVariableData(variableProperty.VariableName);
				if (variableData.Declarations.Count == 1)
				{
					state = PropertyValueState.SingleResult;
					propertyReference = variableData.Declarations[0];
					result = propertyReference.PropertyProvider.GetProperty();
				}
				else
				{
					state = (variableData.Declarations.Count == 0
						? PropertyValueState.NoResult
						: PropertyValueState.MultipleResults);
				}
			}

			if (result != null && result is VariableProperty nestedVariableProperty)
			{
				state = FindVariableEndValue(nestedVariableProperty, styleRule, variableDatabase, out result,
				                             out propertyReference);
			}

			_usedVariableNames.Clear();

			return state;
		}

		private PropertyValueState FindVariableEndValueWithContext(VariableProperty variableProperty,
		                                                           PropertySourceTracker propertySourceTracker,
		                                                           Type expectedType,
		                                                           out IBussProperty result,
		                                                           out PropertyReference
			                                                           propertyReference)
		{
			result = null;
			propertyReference = new PropertyReference(null, null, null);

			while (variableProperty != null && !_usedVariableNames.Contains(variableProperty.VariableName))
			{
				_usedVariableNames.Add(variableProperty.VariableName);
				var usedPropertyReference =
					propertySourceTracker.GetUsedPropertyReference(variableProperty.VariableName, expectedType);
				var propertyProvider = usedPropertyReference.PropertyProvider;
				if (propertyProvider == null)
				{
					_usedVariableNames.Clear();
					return PropertyValueState.NoResult;
				}

				if (propertyProvider.HasVariableReference)
				{
					variableProperty = propertyProvider.GetProperty() as VariableProperty;
				}
				else
				{
					propertyReference = usedPropertyReference;
					result = propertyProvider.GetProperty();
					_usedVariableNames.Clear();
					return PropertyValueState.SingleResult;
				}
			}

			_usedVariableNames.Clear();
			return PropertyValueState.VariableLoopDetected;
		}

		private void RemoveStyleSheet(BussStyleSheet sheet)
		{
			foreach (VariableData variableData in _variables.Values)
			{
				variableData.Declarations.RemoveAll(pr => pr.StyleSheet == sheet);
				variableData.Usages.RemoveAll(pr => pr.StyleSheet == sheet);
			}

			_styleSheets.Remove(sheet);
		}
	}
}
