using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Beamable.UI.Buss.Properties
{
   [BussProperty("font")]
   [System.Serializable]
   public class FontBussProperty : BUSSProperty, IBUSSProperty<FontBussProperty>
   {
      [BussPropertyField("size")]
      public OptionalNumber Size = new OptionalNumber();

      [BussPropertyField("")]
      public OptionalFontAsset FontAsset = new OptionalFontAsset();

      [BussPropertyField("material")]
      public OptionalMaterial FontMaterial = new OptionalMaterial();

      [BussPropertyField("style")]
      public OptionalFontStyle Style = new OptionalFontStyle();

      private List<Optional> AllOptions => new List<Optional>
      {
         Size, FontAsset, FontMaterial, Style
      };

      public FontBussProperty OverrideWith(FontBussProperty other)
      {
         return new FontBussProperty
         {
            Enabled = other?.Enabled ?? Enabled,
            Size = Size.Merge<OptionalNumber, float>(other.Size),
            FontAsset = FontAsset.Merge<OptionalFontAsset, TMP_FontAsset>(other.FontAsset),
            Style = Style.Merge<OptionalFontStyle, FontStyles>(other.Style),
            FontMaterial = FontMaterial.Merge<OptionalMaterial, Material>(other.FontMaterial)
         };
      }

      public FontBussProperty Clone()
      {
         return new FontBussProperty
         {
            Enabled = Enabled,
            Size = Size,
            FontAsset = FontAsset,
            Style = Style,
            FontMaterial = FontMaterial
         };
      }

      protected override bool AnyDefinition => AllOptions.Any(o => o.HasValue);
   }
}