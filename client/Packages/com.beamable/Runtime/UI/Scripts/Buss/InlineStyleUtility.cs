using Beamable.UI.Sdf;
using System;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public static class InlineStyleUtility
	{
		private static  void ChangeInlineProperty<T>(this BussElement element, string key, Action<T> changeFunction) where T : IBussProperty, new()
		{
			var propertyProvider = element.InlineStyle.GetOrCreatePropertyProvider(BussStyle.BackgroundImage.Key);
			if (propertyProvider != null)
			{
				if (propertyProvider.GetProperty() is T property)
				{
					changeFunction(property);
				}
				else
				{
					property = new T();
					changeFunction(property);
				}
				BussConfiguration.UseConfig(c => c.RecalculateInlineStyle(element));
			}
		}

		public static void SetInlineBackgroundImage(this BussElement element, Sprite value)
		{
			element.ChangeInlineProperty<SpriteBussProperty>(BussStyle.BackgroundImage.Key, property =>
			{
				property.Asset = value;
			});
		}

		public static void SetInlineBackgroundColor(this BussElement element, Color value)
		{
			element.ChangeInlineProperty<SingleColorBussProperty>(BussStyle.BackgroundColor.Key, property =>
			{
				property.Color = value;
			});
		}

		public static void SetInlineBackgroundColor(this BussElement element, ColorRect value)
		{
			element.ChangeInlineProperty<VertexColorBussProperty>(BussStyle.BackgroundColor.Key, property =>
			{
				property.ColorRect = value;
			});
		}

		public static void SetInlineFontSize(this BussElement element, float value)
		{
			element.ChangeInlineProperty<FloatBussProperty>(BussStyle.FontSize.Key, property =>
			{
				property.FloatValue = value;
			});
		}

		public static void SetInlineFontColor(this BussElement element, Color value)
		{
			element.ChangeInlineProperty<SingleColorBussProperty>(BussStyle.FontColor.Key, property =>
			{
				property.Color = value;
			});
		}
	}
}
