using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.UI.Buss.Properties;

namespace Beamable.Editor.UI.Buss.Extensions
{

   public class VariableSetWrapper
   {
      public VariableSet VariableSet;
      public FieldInfo Field;
      public VariableScope Scope;

      public void Create(string name)
      {
         VariableSet.CreateEmpty(name);
      }

      public List<VariableWrapper> GetVariables()
      {
         var t = VariableSet.GetType();
         var nameField = t.GetField(nameof(VariableSet<object>._variableNames));
         var valueField = t.GetField(nameof(VariableSet<object>._variableValues));

         var namesRaw = nameField.GetValue(VariableSet);
         var valuesRaw = valueField.GetValue(VariableSet);
         var names = (List<string>) namesRaw;
         var values = (IList) valuesRaw;

         var output = new List<VariableWrapper>();
         for (var i = 0; i < names.Count; i++)
         {
            output.Add(new VariableWrapper
            {
               Name = names[i],
               Value = values[i],
               VariableSet = this
            });
         }
         return output;
      }

   }

   public class VariableWrapper
   {
      public string Name;
      public object Value;

      public VariableSetWrapper VariableSet;

      public Action OnRemoved, OnRenamed;

      public void SetValue(object nextValue)
      {
         var field = VariableSet.VariableSet.GetType().GetField(nameof(VariableSet<object>._variableValues));
         var nameField = VariableSet.VariableSet.GetType().GetField(nameof(VariableSet<object>._variableNames));
         var names = (List<string>) nameField.GetValue(VariableSet.VariableSet);
         var index = names.IndexOf(Name);
         var values = (IList) field.GetValue(VariableSet.VariableSet);
         values[index] = nextValue;
         Value = nextValue;
      }

      public void Rename(string nextName)
      {
         var field = VariableSet.VariableSet.GetType().GetField(nameof(VariableSet<object>._variableNames));
         var names = (List<string>) field.GetValue(VariableSet.VariableSet);
         var index = names.IndexOf(Name);
         names[index] = nextName;
         Name = nextName;
         OnRenamed?.Invoke();
      }


      public void Remove()
      {
         var nameField = VariableSet.VariableSet.GetType().GetField(nameof(VariableSet<object>._variableNames));
         var valueField = VariableSet.VariableSet.GetType().GetField(nameof(VariableSet<object>._variableValues));
         var names = (List<string>) nameField.GetValue(VariableSet.VariableSet);
         var values = (IList) valueField.GetValue(VariableSet.VariableSet);
         var index = names.IndexOf(Name);
         names.RemoveAt(index);
         values.RemoveAt(index);
         OnRemoved?.Invoke();
      }
   }


   public static class VariableScopeExtensions
   {

      public static List<VariableWrapper> GetVariables(this VariableScope self)
      {
         var output = new List<VariableWrapper>();
         foreach (var typeWrapper in self.GetAvailableTypes())
         {
            output.AddRange(typeWrapper.GetVariables());
         }
         return output;
      }

      public static List<VariableSetWrapper> GetAvailableTypes(this VariableScope self)
      {
         var t = self.GetType();
         var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);

         var matchingFields = fields.Where(f => typeof(VariableSet).IsAssignableFrom(f.FieldType));
         return matchingFields.Select(f =>
         {
            return new VariableSetWrapper
            {
               Scope = self,
               VariableSet = (VariableSet) f.GetValue(self),
               Field = f
            };
         }).ToList();
      }
   }
}