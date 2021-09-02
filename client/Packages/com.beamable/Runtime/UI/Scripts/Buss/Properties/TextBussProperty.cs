//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using UnityEngine;
//
//namespace DisruptorBeam.UI.Buss.Properties
//{
//   [System.Serializable]
//   [BussProperty("text")]
//   public class TextBussProperty : BUSSProperty, IBUSSProperty<TextBussProperty>
//   {
//      [BussPropertyField("alignment")]
//      public OptionalTextAlignment TextAlignment = new OptionalTextAlignment();
//
//      private List<Optional> AllOptions => new List<Optional>
//      {
//         TextAlignment
//      };
//
//      public TextBussProperty OverrideWith(TextBussProperty other)
//      {
//         return new TextBussProperty
//         {
//            Enabled = other?.Enabled ?? Enabled,
//            TextAlignment = TextAlignment.Merge<OptionalTextAlignment, _HorizontalAlignmentOptions>(other.TextAlignment)
//         };
//      }
//
//      public TextBussProperty Clone()
//      {
//         return new TextBussProperty
//         {
//            Enabled = Enabled,
//            TextAlignment = TextAlignment
//         };
//      }
//
//      protected override bool AnyDefinition => AllOptions.Any(o => o.HasValue);
//   }
//}