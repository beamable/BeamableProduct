using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;

namespace Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements
{
	public static class BussPropertyVisualElementUtility
	{
		public static BussPropertyVisualElement GetVisualElement(this BussPropertyProvider propertyProvider)
		{
			var property = propertyProvider.GetProperty();
			if (property is FloatBussProperty)
			{
				return new FloatBussPropertyVisualElement(propertyProvider);
			}

			return new NotImplementedBussPropertyVisualElement(propertyProvider);
		}
	}
}
