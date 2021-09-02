using Beamable.UI.Buss;

namespace Beamable.Editor.UI.Buss.Model
{
   public class StyleRuleBundle
   {
      public StyleBundle StyleBundle;
      public StyleBehaviour Behaviour;

      public StyleObject ComputedStyles;

      public StyleRuleBundle(StyleBundle styleBundle, StyleBehaviour behaviour)
      {
         StyleBundle = styleBundle;
         Behaviour = behaviour;
         ComputedStyles = Behaviour?.ComputeStyleObject();
      }

      public Selector Selector => StyleBundle.Selector;
      public StyleObject Style => StyleBundle.Style;
      public StyleSheetObject Sheet => StyleBundle.Sheet;

      public StyleObject SafeComputedStyles => Behaviour?.ComputeStyleObject() ?? Style;

      public string SheetName => Sheet?.name ?? "inline";
   }
}