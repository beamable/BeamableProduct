using System;
using System.Reflection;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.UI.Buss.Properties;

namespace Beamable.Editor.UI.Buss.Model
{
//   public interface IPropertyFieldWrapper
//   {
//      StyleRuleBundle Trace { get; }
//      Type PropertyType { get; }
//      FieldInfo Field { get; }
//      bool IsVariable { get; }
//      void Remove();
//      void Set(object data);
//      object GetValue();
//      string GetName();
//      void Enable();
//      string GetVariable();
//      void ClearVariable();
//      void SetVariable(string wrapperName);
//   }
//
//   public abstract class DefaultPropertyFieldWrapper : IPropertyFieldWrapper
//   {
//      protected readonly BUSSProperty _property;
//      protected readonly FieldInfo _attributeField;
//      public abstract StyleRuleBundle Trace { get; }
//      public abstract Type PropertyType { get; }
//      public abstract bool IsVariable { get; }
//      public abstract FieldInfo Field { get; }
//      public abstract void Remove();
//      public abstract void Set(object data);
//      public abstract object GetValue();
//      public abstract void Enable();
//      public abstract string GetVariable();
//      public abstract void ClearVariable();
//      public abstract void SetVariable(string variable);
//      public DefaultPropertyFieldWrapper(BUSSProperty property, FieldInfo attributeField)
//      {
//         _property = property;
//         _attributeField = attributeField;
//      }
//
//      public string GetName()
//      {
//         var baseName = _attributeField.DeclaringType.GetCustomAttribute<BussPropertyAttribute>()?.Name ??
//            _attributeField.DeclaringType.Name.Replace("Buss", "").Replace("Property", "");
//         var partName = _attributeField.GetCustomAttribute<BussPropertyFieldAttribute>()?.Name ?? _attributeField.Name;
//
//         if (partName.Length > 0)
//         {
//            return $"{baseName}-{partName}";
//         }
//
//         return baseName;
//      }
//
//   }

}