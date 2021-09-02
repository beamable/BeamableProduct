using Beamable.UI.Buss.Properties;
using TMPro;

namespace Beamable.UI.Buss
{
   public class TextStyleBehaviour : StyleBehaviour
   {
      public TextMeshProUGUI TextElement;
      public override string TypeString => "text";

      static TextStyleBehaviour()
      {
         RegisterType<ImageStyleBehaviour>("text");
      }

      public override void ApplyStyleTree()
      {
         base.ApplyStyleTree();

      }

      public override void Apply(StyleObject styles)
      {
         if (TextElement == null) return;

         if (styles.Color.IsDefined())
         {
            var color = styles.Color;

            if (color.Color.TryGetValue(styles, out var c))
            {
               TextElement.color = c;
            }
         }

//         if (styles.Text.IsDefined())
//         {
//            var text = styles.Text;
//            if (text.TextAlignment.TryGetValue(styles, out var hAlign))
//            {
//               TextElement.alignment = (TextAlignmentOptions) ((int)TextElement.alignment & 0xFF00 | (int)hAlign);
//            }
//         }
//
//         if (styles.Vertical.IsDefined())
//         {
//            var vertical = styles.Vertical;
//            if (vertical.Alignment.TryGetValue(styles, out var vAlign))
//            {
//               TextElement.alignment = (TextAlignmentOptions) ((int)TextElement.alignment & 0xFF | (int)vAlign);
//            }
//         }

         if (styles.Font.IsDefined())
         {
            var font = styles.Font;
            if (font.Size.TryGetValue(styles, out var size))
            {
               TextElement.fontSize = size;
            }

            if (font.Style.TryGetValue(styles, out var style))
            {
               TextElement.fontStyle = style;
            }

            if (font.FontAsset.TryGetValue(styles, out var asset))
            {
               TextElement.font = asset;
            }

            if (font.FontMaterial.TryGetValue(styles, out var material))
            {
               TextElement.fontSharedMaterial = material;
            }
         }

         TextElement.SetAllDirty();
      }
   }
}