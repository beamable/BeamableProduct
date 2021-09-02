using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.UI.Buss.Properties
{
   [System.Serializable]
   [BussProperty("background")]
   public class BackgroundBussProperty : BUSSProperty, IBUSSProperty<BackgroundBussProperty>
   {
      [BussPropertyField("color")]
      public OptionalColor Color = new OptionalColor();

      [BussPropertyField("image")]
      public OptionalTexture Texture = new OptionalTexture();

      [BussPropertyField("tint")]
      public OptionalColor TextureColor = new OptionalColor();

      [BussPropertyField("image-scale")]
      public OptionalVector2 TextureScale = new OptionalVector2();

      [BussPropertyField("image-offset")]
      public OptionalVector2 TextureOffset = new OptionalVector2();

      [BussPropertyField("shape")]
      public OptionalSprite Shape = new OptionalSprite();

      [Range(0, 1)]
      [BussPropertyField("gradient-amount")]
      public OptionalNumber GradientAmount = new OptionalNumber();

      [Range(0,10)]
      [BussPropertyField("gradient-scale")]
      public OptionalNumber GradientScale = new OptionalNumber();

      [Range(0, 360)]
      [BussPropertyField("gradient-angle")]
      public OptionalNumber GradientAngle = new OptionalNumber();

      [Range(-2, 2)]
      [BussPropertyField("gradient-offset")]
      public OptionalNumber GradientOffset = new OptionalNumber();

      [BussPropertyField("gradient-start")]
      public OptionalColor GradientStartColor = new OptionalColor();

      [BussPropertyField("gradient-end")]
      public OptionalColor GradientEndColor = new OptionalColor();

      private List<Optional> AllOptions => new List<Optional>
      {
         Color, Texture, TextureColor, TextureScale, TextureOffset, GradientAmount, GradientScale, GradientAngle,
         GradientOffset, GradientStartColor, GradientEndColor
      };

      public BackgroundBussProperty OverrideWith(BackgroundBussProperty other)
      {
         return new BackgroundBussProperty
         {
            Enabled = other?.Enabled ?? Enabled,
            Color = Color.Merge<OptionalColor, Color>(other.Color),
            Texture = Texture.Merge<OptionalTexture, Texture2D>(other.Texture),
            Shape = Shape.Merge<OptionalSprite, Sprite>(other.Shape),
            TextureColor = TextureColor.Merge<OptionalColor, Color>(other.TextureColor),
            TextureOffset = TextureOffset.Merge<OptionalVector2, Vector2>(other.TextureOffset),
            TextureScale = TextureScale.Merge<OptionalVector2, Vector2>(other.TextureScale),
            GradientAmount = GradientAmount.Merge<OptionalNumber, float>(other.GradientAmount),
            GradientAngle = GradientAngle.Merge<OptionalNumber, float>(other.GradientAngle),
            GradientScale = GradientScale.Merge<OptionalNumber, float>(other.GradientScale),
            GradientOffset = GradientOffset.Merge<OptionalNumber, float>(other.GradientOffset),
            GradientEndColor = GradientEndColor.Merge<OptionalColor, Color>(other.GradientEndColor),
            GradientStartColor = GradientStartColor.Merge<OptionalColor, Color>(other.GradientStartColor),
         };
      }

      public BackgroundBussProperty Clone()
      {
         return new BackgroundBussProperty
         {
            Enabled = Enabled,
            Color = Color,
            Texture = Texture,
            TextureColor = TextureColor,
            TextureOffset = TextureOffset,
            TextureScale = TextureScale,
            GradientAmount = GradientAmount,
            GradientEndColor = GradientEndColor,
            GradientStartColor = GradientStartColor,
            GradientAngle = GradientAngle,
            GradientOffset = GradientOffset,
            GradientScale = GradientScale,
            Shape = Shape
         };
      }

      protected override bool AnyDefinition => AllOptions.Any(o => o.HasValue);
   }

}