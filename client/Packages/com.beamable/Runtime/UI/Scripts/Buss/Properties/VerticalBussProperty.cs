//using TMPro;
//
//namespace DisruptorBeam.UI.Buss.Properties
//{
//   [BussProperty("vertical")]
//   [System.Serializable]
//   public class VerticalBussProperty : BUSSProperty, IBUSSProperty<VerticalBussProperty>
//   {
//      [BussPropertyField("align")]
//      public OptionalVerticalAlignment Alignment = new OptionalVerticalAlignment();
//
//      public VerticalBussProperty OverrideWith(VerticalBussProperty other)
//      {
//         return new VerticalBussProperty
//         {
//            Enabled = other?.Enabled ?? Enabled,
//            Alignment = Alignment.Merge<OptionalVerticalAlignment, _VerticalAlignmentOptions>(other.Alignment)
//         };
//      }
//
//      public VerticalBussProperty Clone()
//      {
//         return new VerticalBussProperty
//         {
//            Enabled = Enabled,
//            Alignment = Alignment
//         };
//      }
//
//      protected override bool AnyDefinition => Alignment.HasValue;
//   }
//}