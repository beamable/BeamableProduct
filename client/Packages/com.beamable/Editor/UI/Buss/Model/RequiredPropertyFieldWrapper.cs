using System;
using System.Reflection;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;

namespace Beamable.Editor.UI.Buss.Model
{
//   public class RequiredPropertyFieldWrapper : DefaultPropertyFieldWrapper
//   {
//      public override StyleRuleBundle Trace { get; }
//
//      public RequiredPropertyFieldWrapper(BUSSProperty property, FieldInfo valueField, StyleRuleBundle trace) : base(property, valueField)
//      {
//         Trace = trace;
//      }
//
//      public override bool Equals(object obj)
//      {
//         if (obj is RequiredPropertyFieldWrapper other)
//         {
//            return Equals(other);
//         }
//         return false;
//      }
//
//      public bool Equals(RequiredPropertyFieldWrapper other)
//      {
//         var e = _property == other._property;
//         return e;
//      }
//
//      public override Type PropertyType => _attributeField.FieldType;
//      public override FieldInfo Field => _attributeField;
//
//      public override void Remove()
//      {
//         _property.Enabled = false;
//      }
//
//      public override void Set(object data)
//      {
//         _attributeField.SetValue(_property, data);
//         // TODO: find a better way to do this.
//         StyleBehaviourExtensions.Refresh();
//
//      }
//
//      public override object GetValue()
//      {
//         return _attributeField.GetValue(_property);
//      }
//
//      public override void Enable()
//      {
//         _property.Enabled = true;
//      }
//   }

}