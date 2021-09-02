using System;
using System.Reflection;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.UI.Buss.Properties;

namespace Beamable.Editor.UI.Buss.Model
{
   public class OptionalPropertyFieldWrapper
   {
      private readonly BUSSProperty _property;
      private readonly Optional _optional;
      private readonly FieldInfo _optionalField;
      private readonly FieldInfo _valueField;
      private MethodInfo _getValueMethod, _setValueMethod;
      public Type PropertyType => _valueField.FieldType;
      public bool IsVariable => !string.IsNullOrEmpty(_optional.VariableReference);
      public FieldInfo Field => _optionalField;


      public string Name => GetName();

      public StyleRuleBundle Trace { get; }

      public OptionalPropertyFieldWrapper(BUSSProperty property, Optional optional, FieldInfo optionalField, FieldInfo valueField, StyleRuleBundle trace)
      {
         _property = property;
         _optional = optional;
         _optionalField = optionalField;
         _valueField = valueField;
         Trace = trace;

         _getValueMethod = optional.GetType().GetMethod(nameof(Optional<object>.GetValue));
         _setValueMethod = optional.GetType().GetMethod(nameof(Optional<object>.Set));
      }

      public void Remove()
      {
         _optional.HasValue = false;
         _optional.VariableReference = null;
      }

      public void Set(object data)
      {
         _setValueMethod.Invoke(_optional, new object[] {true, data});
         StyleBehaviourExtensions.Refresh();
      }

      public object GetValue(IVariableScope scope)
      {
         return _getValueMethod.Invoke(_optional, new object[]{scope});
      }


      public void Enable()
      {
         _optional.HasValue = true;
         _property.Enabled = true;
         _optional.VariableReference = null;

      }

      public string GetVariable()
      {
         return _optional.VariableReference;
      }

      public void ClearVariable()
      {
         _optional.VariableReference = null;
      }

      public void SetVariable(string variable)
      {
         _optional.VariableReference = variable;
      }

      public string GetName()
      {
         var baseName = _optionalField.DeclaringType.GetCustomAttribute<BussPropertyAttribute>()?.Name ??
                        _optionalField.DeclaringType.Name.Replace("Buss", "").Replace("Property", "");
         var partName = _optionalField.GetCustomAttribute<BussPropertyFieldAttribute>()?.Name ?? _optionalField.Name;

         if (partName.Length > 0)
         {
            return $"{baseName}-{partName}";
         }

         return baseName;
      }
   }
}