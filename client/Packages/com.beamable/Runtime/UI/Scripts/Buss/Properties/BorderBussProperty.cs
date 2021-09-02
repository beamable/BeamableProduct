using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss.Properties
{
   [BussProperty("border")]
   [System.Serializable]
   public class BorderBussProperty : BUSSProperty, IBUSSProperty<BorderBussProperty>
   {
      protected override bool AnyDefinition { get; }

      [BussPropertyField("width")]
      [Range(0, 1)]
      public OptionalNumber Width = new OptionalNumber();

      [BussPropertyField("color")]
      public OptionalColor Color = new OptionalColor();

      [BussPropertyField("radius")]
      [Range(0, 1)]
      public OptionalNumber Radius = new OptionalNumber();

      public BorderBussProperty OverrideWith(BorderBussProperty other)
      {
         return new BorderBussProperty
         {
            Enabled = other?.Enabled ?? Enabled,

            Width = Width.Merge<OptionalNumber, float>(other.Width),
            Radius = Radius.Merge<OptionalNumber, float>(other.Radius),
            Color = Color.Merge<OptionalColor, Color>(other.Color)
         };
      }

      public BorderBussProperty Clone()
      {
         return new BorderBussProperty
         {
            Enabled = Enabled,
            Width = Width,
            Radius = Radius,
            Color = Color
         };
      }
   }
}