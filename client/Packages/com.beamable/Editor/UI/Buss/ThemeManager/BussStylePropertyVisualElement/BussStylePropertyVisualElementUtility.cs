using Beamable.UI.Buss;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.UI.Components
{
	public static class BussStylePropertyVisualElementUtility
	{
		private static HashSet<string> _usedVariableNames = new HashSet<string>();

		public static PropertyValueState TryGetProperty(BussPropertyProvider basePropertyProvider,
														BussStyleDescription styleRule,
														VariableDatabase variableDatabase,
														PropertySourceTracker context,
														out IBussProperty result,
														out VariableDatabase.PropertyReference variablePropertyReference)
		{
			if (!basePropertyProvider.HasVariableReference)
			{
				variablePropertyReference = new VariableDatabase.PropertyReference(null, null, null);
				result = basePropertyProvider.GetProperty();
				return PropertyValueState.SingleResult;
			}
			else
			{
				if (context != null)
				{
					return FindVariableEndValueWithContext((VariableProperty)basePropertyProvider.GetProperty(),
														   context, BussStyle.GetBaseType(basePropertyProvider.Key), out result, out variablePropertyReference);
				}
				return FindVariableEndValue((VariableProperty)basePropertyProvider.GetProperty(),
					styleRule, variableDatabase, out result, out variablePropertyReference);
			}
		}

		/// <summary>
		/// Searches for the variable value without context.
		/// If there is a variable in the same StyleRule, then it is returned.
		/// If there is only one declaration of a variable in variable database, it returns the value of it.
		/// Otherwise returns null.
		/// It can search for end value recursively.
		/// </summary>
		private static PropertyValueState FindVariableEndValue(VariableProperty variableProperty,
															   BussStyleDescription styleRule,
															   VariableDatabase variableDatabase,
															   out IBussProperty result,
															   out VariableDatabase.PropertyReference propertyReference)
		{
			result = null;
			propertyReference = new VariableDatabase.PropertyReference(null, null, null);
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
				state = FindVariableEndValue(nestedVariableProperty, styleRule, variableDatabase, out result, out propertyReference);
			}

			_usedVariableNames.Clear();

			return state;
		}

		private static PropertyValueState FindVariableEndValueWithContext(VariableProperty variableProperty,
																		  PropertySourceTracker context,
																		  Type expectedType,
																		  out IBussProperty result,
																		  out VariableDatabase.PropertyReference
																			  propertyReference)
		{
			result = null;
			propertyReference = new VariableDatabase.PropertyReference(null, null, null);

			while (!_usedVariableNames.Contains(variableProperty.VariableName))
			{
				_usedVariableNames.Add(variableProperty.VariableName);
				var usedPropertyReference = context.GetUsedPropertyReference(variableProperty.VariableName, expectedType);
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

		public enum PropertyValueState
		{
			NoResult,
			SingleResult,
			MultipleResults,
			VariableLoopDetected
		}
	}
}
