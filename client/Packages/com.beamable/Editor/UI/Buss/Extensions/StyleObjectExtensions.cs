using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;

namespace Beamable.Editor.UI.Buss.Extensions
{
   // reflection is okay in the editor space.
   public static class StyleObjectExtensions
   {
      public static IEnumerable<OptionalPropertyFieldWrapper> GetUnusedProperties(this StyleObject self)
      {
         var bussProps = self.GetProperties();
         foreach (var bussProp in bussProps)
         {
            foreach (var prop in bussProp.GenerateProperties(false, true))
            {
               yield return prop;
            }
         }
      }

      public static IEnumerable<OptionalPropertyFieldWrapper> GetUsedProperties(this StyleObject self)
      {
         var bussProps = self.GetProperties();
         foreach (var bussProp in bussProps)
         {
            foreach (var prop in bussProp.GenerateProperties(true, false))
            {
               yield return prop;
            }
         }
      }

      public static List<BUSSProperty> GetProperties(this StyleObject self)
      {
         var type = self.GetType();
         var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
         var bussFields = allFields.Where(f => typeof(BUSSProperty).IsAssignableFrom(f.FieldType));
         var values = bussFields.Select(f => f.GetValue(self) as BUSSProperty).Where(p => p != null);
         return values.ToList();
      }
   }
}