using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;

namespace Beamable.Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements
{
	public static class BussPropertyVisualElementUtility
	{
		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider)
		{
			var property = propertyProvider.GetProperty();
			return GetVisualElement(property);
		}

		private static BussPropertyVisualElement GetVisualElement(this IBussProperty property)
		{
			if (property is FloatBussProperty floatProperty)
			{
				return new FloatBussPropertyVisualElement(floatProperty);
			}

			if (property is SingleColorBussProperty colorProperty)
			{
				return new ColorButtPropertyVisualElement(colorProperty);
			}

			if (property is VertexColorBussProperty vertexColorProperty)
			{
				return new VertexColorBussPropertyVisualElement(vertexColorProperty);
			}

			if (property is TextAlignmentOptionsBussProperty textAlignmentProperty)
			{
				return new TextAlignmentBussPropertyVisualElement(textAlignmentProperty);
			}

			if (property is EnumBussProperty enumBussProperty)
			{
				return new EnumBussPropertyVisualElement(enumBussProperty);
			}

			if (property is BaseAssetProperty assetProperty)
			{
				return new AssetBussPropertyVisualElement(assetProperty);
			}

			return new NotImplementedBussPropertyVisualElement(property);
		}

		private static HashSet<string> _visitedVariables = new HashSet<string>();

		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider,
		                                                         VariableDatabase variableDatabase,
		                                                         BussStyleRule context,
		                                                         Type baseType = null)
		{
			return propertyProvider.GetProperty().GetEndProperty(variableDatabase, context, baseType)?.GetVisualElement() ??
			       new NoValidVariableBussPropertyVisualElement(propertyProvider.GetProperty());
		}
		
		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider,
		                                                         VariableDatabase variableDatabase,
		                                                         Type baseType = null)
		{
			return propertyProvider.GetProperty().GetEndProperty(variableDatabase, baseType)?.GetVisualElement() ??
			       new NoValidVariableBussPropertyVisualElement(propertyProvider.GetProperty());
		}

		public static IBussProperty GetEndProperty(this IBussProperty property,
		                                           VariableDatabase variableDatabase,
		                                           BussStyleRule context,
		                                           Type baseType = null)
		{
			if (property is VariableProperty variableProperty && 
			    context != null &&
			    BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
			{
				var propertyInContext = context.Properties.FirstOrDefault(p => p.Key == variableProperty.VariableName);
				if (propertyInContext != null)
				{
					return propertyInContext.GetProperty();
				}
			}

			return property.GetEndProperty(variableDatabase, baseType);
		}

		public static IBussProperty GetEndProperty(this IBussProperty property,
		                                           VariableDatabase variableDatabase,
		                                           Type baseType = null)
		{
			if (baseType == null)
			{
				baseType = typeof(IBussProperty);
			}

			if (property is VariableProperty variableProperty &&
			    !_visitedVariables.Contains(variableProperty.VariableName))
			{
				if (BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
				{
					_visitedVariables.Add(variableProperty.VariableName);
					var variableData = variableDatabase.GetVariableData(variableProperty.VariableName);
					if (variableData != null)
					{
						foreach (var reference in variableData.Declarations)
						{
							if (reference != null)
							{
								_visitedVariables.Clear();
								var endProperty = reference
								                  .propertyProvider.GetProperty()
								                  .GetEndProperty(variableDatabase, baseType);
								if (baseType.IsInstanceOfType(endProperty))
								{
									return endProperty;
								}
							}
						}
					}
				}

				return null;
			}

			_visitedVariables.Clear();
			return property;
		}
	}
}
