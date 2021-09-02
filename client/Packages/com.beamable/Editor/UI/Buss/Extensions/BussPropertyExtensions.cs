using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss.Properties;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Extensions
{
   public static class BussPropertyExtensions
   {
      public static IEnumerable<OptionalPropertyFieldWrapper> GenerateProperties(this BUSSProperty self, bool onlyEnabled=true, bool onlyDisabled=false, StyleRuleBundle trace=null)
      {

         var type = self.GetType();
         var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
         var fields = allFields.Where(f => !f.Name.Equals(nameof(BUSSProperty.Enabled)));

         var optionalFields = fields.Where(f => typeof(Optional).IsAssignableFrom(f.FieldType));


         foreach (var optionalField in optionalFields)
         {
            var optionalValue = optionalField.GetValue(self) as Optional;

            var disabled = !(optionalValue?.HasValue ?? false);
            if (onlyEnabled && (disabled || !self.Enabled )) continue; // there no value here.
            if (onlyDisabled && (!disabled && self.Enabled)) continue; // there no value here.

            var valueField = optionalValue.GetType().GetField(nameof(Optional<object>.Value)); // TODO: Don't access the field directly, use the GetValue(), so that variables are supported.
            if (valueField != null)
            {
               yield return new OptionalPropertyFieldWrapper(self, optionalValue, optionalField, valueField, trace);
            }
         }
      }

      public static IEnumerable<RequiredBUSSPropertyVisualElement> GenerateElements(this BUSSProperty self, StyleRuleBundle model)
      {
         foreach (var wrapper in self.GenerateProperties(true, false, model))
         {
            yield return new RequiredBUSSPropertyVisualElement(wrapper, model);
         }
      }
   }
}