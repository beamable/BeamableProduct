using System.Collections.Generic;
using System.Linq;
using Beamable.UI.Buss;

namespace Beamable.Editor.UI.Buss.Extensions
{
   public static class StyleSheetObjectExtensions
   {
      public static List<VariableWrapper> GetVariables(this StyleSheetObject self)
      {
         var mergedScope = self.Rules
            .Select(r => r.Style.Scope)
            .Aggregate((agg, curr) => agg.Merge(curr));

         return mergedScope.GetVariables();
      }
   }
}