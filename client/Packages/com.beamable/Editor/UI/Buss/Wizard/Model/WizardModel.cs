using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
using Beamable.Editor.UI.Buss.Model;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Wizard.Model
{

   public class WizardModel
   {
      public bool HasThemeName => (IsNewTheme && !string.IsNullOrEmpty(ThemeName)) || (!IsNewTheme && Sheet != null);
      public string ThemeName;
      public StyleSheetObject Sheet;
      public bool IsNewTheme;

      public OptionalColor PrimaryColor = new OptionalColor();
      public OptionalColor SecondaryColor = new OptionalColor();

      public StyleSheetObject Complete()
      {
         if (IsNewTheme)
         {
            var so = StyleSheetObject.CreateInstance<StyleSheetObject>();
            so.name = ThemeName;

            Apply(so);
            AssetDatabase.CreateAsset(so, $"Assets/{ThemeName}.asset");

            // automatically add it to the BUSSConfiguration.
            BussConfiguration.Instance.DefaultSheets.Add(so);
            EditorUtility.SetDirty(BussConfiguration.Instance);
            EditorUtility.SetDirty(so);

            StyleBehaviourExtensions.Refresh();
            return so;
         }
         else
         {
            Apply(Sheet);
            EditorUtility.SetDirty(Sheet);
            EditorUtility.SetDirty(BussConfiguration.Instance);
            StyleBehaviourExtensions.Refresh();
            return null;
         }

      }
      public void Apply(StyleSheetObject sheet)
      {
         var rootRule = GetRootRule(sheet);
         rootRule.Style.Scope.Colors.Set("primary", PrimaryColor.GetValue(null));
         rootRule.Style.Scope.Colors.Set("secondary", SecondaryColor.GetValue(null));
      }

      public SelectorWithStyle GetRootRule(StyleSheetObject sheet)
      {
         var rootRule = sheet.Rules.FirstOrDefault(x => x.Selector.ToString().Equals(":root"));
         if (rootRule == null)
         {
            rootRule = new SelectorWithStyle
            {
               Selector = SelectorParser.Parse(":root"),
               Style = new StyleObject()
            };
            sheet.Rules.Add(rootRule);
         }
         return rootRule;
      }

      public void ClearSheet()
      {
         IsNewTheme = true;
         ThemeName = null;
         Sheet = null;
         PrimaryColor.Value = Color.black;
         SecondaryColor.Value = Color.black;
      }

      public void SetFromSheet(StyleSheetObject styleSheetObject)
      {
         Sheet = styleSheetObject;
         IsNewTheme = false;
         ThemeName = styleSheetObject.name;

         var rootRule = Sheet.Rules.FirstOrDefault(x => x.Selector.ToString().Equals(":root"));

         var pColor = Color.white;
         if (rootRule?.Style.Scope.TryResolve<Color>("primary", out pColor) ?? false)
         {
            PrimaryColor.Set(true, pColor);
         }

         var sColor = Color.white;
         if (rootRule?.Style.Scope.TryResolve<Color>("secondary", out sColor) ?? false)
         {
            SecondaryColor.Set(true, sColor);
         }

      }
   }

}