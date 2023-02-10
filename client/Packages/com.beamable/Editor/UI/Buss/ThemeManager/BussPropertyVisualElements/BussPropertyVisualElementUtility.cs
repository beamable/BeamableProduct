using Beamable.Editor.UI.Components;

namespace Beamable.UI.Buss
{
	public static class BussPropertyVisualElementUtility
	{
		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider, StylePropertyModel model)
		{
			var property = propertyProvider.GetProperty();
			return GetVisualElement(property, model);
		}

		public static BussPropertyVisualElement GetVisualElement(this IBussProperty property, StylePropertyModel model)
		{
			switch (property)
			{
				case FloatBussProperty floatProperty:
					return new FloatBussPropertyVisualElement(floatProperty);
				case Vector2BussProperty vector2BussProperty:
					return new Vector2BussPropertyVisualElement(vector2BussProperty);
				case SingleColorBussProperty colorProperty:
					return new ColorBussPropertyVisualElement(colorProperty);
				case RectTransformProperty rectProperty:
					return new RectTransformBussPropertyVisualElement(rectProperty);
				case VertexColorBussProperty vertexColorProperty:
					return new VertexColorBussPropertyVisualElement(vertexColorProperty);
				case TextAlignmentOptionsBussProperty textAlignmentProperty:
					return new TextAlignmentBussPropertyVisualElement(textAlignmentProperty);
				case EnumBussProperty enumBussProperty:
					return new EnumBussPropertyVisualElement(enumBussProperty);
				case BaseAssetProperty assetProperty:
					return new AssetBussPropertyVisualElement(assetProperty);
				case IComputedProperty computedProperty:
					return new ComputedBussPropertyVisualElement(computedProperty, model);
				default:
					return new NotImplementedBussPropertyVisualElement(property);
			}
		}
	}
}
