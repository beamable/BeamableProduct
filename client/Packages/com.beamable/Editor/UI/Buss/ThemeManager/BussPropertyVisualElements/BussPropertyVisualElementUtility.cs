using Beamable.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.UI.Buss
{
	public static class BussPropertyVisualElementUtility
	{
		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider)
		{
			var property = propertyProvider.GetProperty();
			return GetVisualElement(property);
		}

		public static BussPropertyVisualElement GetVisualElement(this IBussProperty property)
		{
			switch (property)
			{
				case FloatBussProperty floatProperty:
					return new FloatBussPropertyVisualElement(floatProperty);
				case Vector2BussProperty vector2BussProperty:
					return new Vector2BussPropertyVisualElement(vector2BussProperty);
				case SingleColorBussProperty colorProperty:
					return new ColorBussPropertyVisualElement(colorProperty);
				case VertexColorBussProperty vertexColorProperty:
					return new VertexColorBussPropertyVisualElement(vertexColorProperty);
				case TextAlignmentOptionsBussProperty textAlignmentProperty:
					return new TextAlignmentBussPropertyVisualElement(textAlignmentProperty);
				case EnumBussProperty enumBussProperty:
					return new EnumBussPropertyVisualElement(enumBussProperty);
				case BaseAssetProperty assetProperty:
					return new AssetBussPropertyVisualElement(assetProperty);
				default:
					return new NotImplementedBussPropertyVisualElement(property);
			}
		}

		// private static readonly HashSet<string> _visitedVariables = new HashSet<string>();

		// public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider,
		//                                                          VariableDatabase variableDatabase,
		//                                                          BussStyleRule context,
		//                                                          out BussStyleSheet variableSource,
		//                                                          Type baseType = null)
		// {
		// 	return propertyProvider.GetProperty()
		// 	                       .GetEndProperty(variableDatabase, context, out variableSource, baseType)
		// 	                       ?.GetVisualElement() ??
		// 	       new NoValidVariableBussPropertyVisualElement(propertyProvider.GetProperty());
		// }

		// public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider,
		//                                                          VariableDatabase variableDatabase,
		//                                                          out BussStyleSheet variableSource,
		//                                                          Type baseType = null)
		// {
		// 	return propertyProvider.GetProperty().GetEndProperty(variableDatabase, out variableSource, baseType)
		// 	                       ?.GetVisualElement() ??
		// 	       new NoValidVariableBussPropertyVisualElement(propertyProvider.GetProperty());
		// }

		// public static IBussProperty GetEndProperty(this IBussProperty property,
		//                                            VariableDatabase variableDatabase,
		//                                            BussStyleRule context,
		//                                            out BussStyleSheet variableSource,
		//                                            Type baseType = null)
		// {
		// 	if (property is VariableProperty variableProperty &&
		// 	    context != null &&
		// 	    BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
		// 	{
		// 		var propertyInContext = context.Properties.FirstOrDefault(p => p.Key == variableProperty.VariableName);
		// 		if (propertyInContext != null)
		// 		{
		// 			variableSource = null;
		// 			return propertyInContext.GetProperty();
		// 		}
		// 	}
		//
		// 	return property.GetEndProperty(variableDatabase, out variableSource, baseType);
		// }

		// public static IBussProperty GetEndProperty(this IBussProperty property,
		//                                            VariableDatabase variableDatabase,
		//                                            out BussStyleSheet variableSource,
		//                                            Type baseType = null)
		// {
		// 	if (baseType == null)
		// 	{
		// 		baseType = typeof(IBussProperty);
		// 	}
		//
		// 	if (property is VariableProperty variableProperty &&
		// 	    !_visitedVariables.Contains(variableProperty.VariableName))
		// 	{
		// 		if (BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
		// 		{
		// 			_visitedVariables.Add(variableProperty.VariableName);
		// 			var variableData = variableDatabase.GetVariableData(variableProperty.VariableName);
		// 			if (variableData != null)
		// 			{
		// 				foreach (var reference in variableData.Declarations)
		// 				{
		// 					if (reference.StyleSheet != null)
		// 					{
		// 						_visitedVariables.Clear();
		// 						var endProperty = reference
		// 						                  .PropertyProvider.GetProperty()
		// 						                  .GetEndProperty(variableDatabase, out variableSource, baseType);
		// 						if (baseType.IsInstanceOfType(endProperty))
		// 						{
		// 							if (variableSource == null)
		// 							{
		// 								variableSource = reference.StyleSheet;
		// 							}
		//
		// 							return endProperty;
		// 						}
		// 					}
		// 				}
		// 			}
		// 		}
		//
		// 		variableSource = null;
		// 		return null;
		// 	}
		//
		// 	_visitedVariables.Clear();
		// 	variableSource = null;
		// 	return property;
		// }
	}
}
