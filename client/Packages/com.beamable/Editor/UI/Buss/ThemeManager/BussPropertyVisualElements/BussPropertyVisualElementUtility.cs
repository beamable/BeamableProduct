using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;

namespace Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements
{
	public static class BussPropertyVisualElementUtility
	{
		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider)
		{
			var property = propertyProvider.GetProperty();
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
	}
}
