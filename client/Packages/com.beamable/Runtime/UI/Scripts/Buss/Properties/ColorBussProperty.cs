using UnityEngine;

namespace Beamable.UI.Buss.Properties
{

   [BussProperty("color")]
   [System.Serializable]
   public class ColorBussProperty : BUSSProperty, IBUSSProperty<ColorBussProperty>
   {
      [BussPropertyField("")]
      public OptionalColor Color = new OptionalColor();

      protected override bool AnyDefinition => Color.HasValue;

      public ColorBussProperty OverrideWith(ColorBussProperty other)
      {
         return new ColorBussProperty
         {
            Enabled = other?.Enabled ?? Enabled,
            Color = Color.Merge<OptionalColor, Color>(other.Color),
         };
      }

      public ColorBussProperty Clone()
      {
         return new ColorBussProperty
         {
            Enabled = Enabled,
            Color = Color
         };
      }

   }

}