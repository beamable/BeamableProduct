using Beamable.UI.Buss;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.UI.Components
{
	public static class StylePropertyVisualElementUtility
	{
		private static readonly HashSet<string> UsedVariableNames = new HashSet<string>();

		public static VariableDatabase.PropertyValueState TryGetProperty(BussPropertyProvider basePropertyProvider,
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
				return VariableDatabase.PropertyValueState.SingleResult;
			}

			if (context != null)
			{
				return FindVariableEndValueWithContext((VariableProperty)basePropertyProvider.GetProperty(),
				                                       context, BussStyle.GetBaseType(basePropertyProvider.Key), out result, out variablePropertyReference);
			}
			return FindVariableEndValue((VariableProperty)basePropertyProvider.GetProperty(),
			                            styleRule, variableDatabase, out result, out variablePropertyReference);
		}

		/// <summary>
		/// Searches for the variable value without context.
		/// If there is a variable in the same StyleRule, then it is returned.
		/// If there is only one declaration of a variable in variable database, it returns the value of it.
		/// Otherwise returns null.
		/// It can search for end value recursively.
		/// </summary>
		private static VariableDatabase.PropertyValueState FindVariableEndValue(VariableProperty variableProperty,
															   BussStyleDescription styleRule,
															   VariableDatabase variableDatabase,
															   out IBussProperty result,
															   out VariableDatabase.PropertyReference propertyReference)
		{
			result = null;
			propertyReference = new VariableDatabase.PropertyReference(null, null, null);
			VariableDatabase.PropertyValueState state;

			if (UsedVariableNames.Contains(variableProperty.VariableName)) // check if we are not in infinite loop
			{
				UsedVariableNames.Clear();
				return VariableDatabase.PropertyValueState.VariableLoopDetected;
			}

			UsedVariableNames.Add(variableProperty.VariableName);

			if (styleRule.HasProperty(variableProperty.VariableName))
			{
				state = VariableDatabase.PropertyValueState.SingleResult;
				result = styleRule.GetProperty(variableProperty.VariableName);
			}
			else
			{
				var variableData = variableDatabase.GetVariableData(variableProperty.VariableName);
				if (variableData.Declarations.Count == 1)
				{
					state = VariableDatabase.PropertyValueState.SingleResult;
					propertyReference = variableData.Declarations[0];
					result = propertyReference.PropertyProvider.GetProperty();
				}
				else
				{
					state = (variableData.Declarations.Count == 0
						? VariableDatabase.PropertyValueState.NoResult
						: VariableDatabase.PropertyValueState.MultipleResults);
				}
			}

			if (result != null && result is VariableProperty nestedVariableProperty)
			{
				state = FindVariableEndValue(nestedVariableProperty, styleRule, variableDatabase, out result, out propertyReference);
			}

			UsedVariableNames.Clear();

			return state;
		}

		private static VariableDatabase.PropertyValueState FindVariableEndValueWithContext(VariableProperty variableProperty,
																		  PropertySourceTracker context,
																		  Type expectedType,
																		  out IBussProperty result,
																		  out VariableDatabase.PropertyReference
																			  propertyReference)
		{
			result = null;
			propertyReference = new VariableDatabase.PropertyReference(null, null, null);

			while (!UsedVariableNames.Contains(variableProperty.VariableName))
			{
				UsedVariableNames.Add(variableProperty.VariableName);
				var usedPropertyReference = context.GetUsedPropertyReference(variableProperty.VariableName, expectedType);
				var propertyProvider = usedPropertyReference.PropertyProvider;
				if (propertyProvider == null)
				{
					UsedVariableNames.Clear();
					return VariableDatabase.PropertyValueState.NoResult;
				}
				if (propertyProvider.HasVariableReference)
				{
					variableProperty = propertyProvider.GetProperty() as VariableProperty;
				}
				else
				{
					propertyReference = usedPropertyReference;
					result = propertyProvider.GetProperty();
					UsedVariableNames.Clear();
					return VariableDatabase.PropertyValueState.SingleResult;
				}
			}

			UsedVariableNames.Clear();
			return VariableDatabase.PropertyValueState.VariableLoopDetected;
		}
	}
}
