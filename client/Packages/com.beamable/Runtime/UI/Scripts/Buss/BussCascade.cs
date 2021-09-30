using System.Collections.Generic;
using System.Linq;

namespace Beamable.UI.Buss
{
   public static class BussCascade
   {
      /// <summary>
      /// Assumes that start is the root of the world. No parent styles will be applied
      /// </summary>
      /// <param name="start"></param>
      public static void Cascade(StyleBehaviour start)
      {
         var config = BussConfiguration.Instance;

         var baseRules = new List<SelectorWithStyle>();
         foreach (var sheet in config.EnumerateSheets())
         {
            foreach (var rule in sheet.Rules)
            {
               baseRules.Add(rule);
            }
         }

         void Apply(StyleBehaviour element, StyleObject inherit, List<SelectorWithStyle> parentRules)
         {
            var selfRules = parentRules.ToList();
            foreach (var sheet in element.StyleSheets)
            {
               foreach (var selfRule in sheet.Rules)
               {
                  selfRules.Add(selfRule);
               }
            }
            var directMatches = selfRules.Where(r => element.IsDirectMatch(r.Selector)).ToList();

            // sort them by selector weight
            // TODO take into account sheet distance.
            directMatches.Sort((a, b) => a.Selector.Weight().CompareTo(b.Selector.Weight()));
            var styles = directMatches.Select(r => r.Style).ToList();
            var direct = styles.Count == 0
               ? null
               : styles.Aggregate((agg, curr) => agg.Merge(curr));

            var final = inherit == null
               ? direct
               : inherit.Merge(direct);

            // element.Apply(final);
            element.SetStyles(final, inherit, direct);

            foreach (var child in element.GetChildren())
            {
               Apply(child, final, selfRules);
            }
         }

         Apply(start, null, baseRules);
      }
   }
}