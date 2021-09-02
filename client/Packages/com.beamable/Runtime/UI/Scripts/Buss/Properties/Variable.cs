using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.UI.Buss.Properties
{
   [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
   public class VariableTypeNameAttribute : Attribute
   {
      public string Name { get; }

      public VariableTypeNameAttribute(string name)
      {
         Name = name;
      }
   }

   public interface ISupportsReference<T>
   {
      T GetValue(IVariableScope scope);
   }

   public interface IVariableScope
   {
      T Resolve<T>(string variable);
   }

   [System.Serializable]
   public class VariableScope : IVariableScope
   {
      public NumberVariableSet Numbers = new NumberVariableSet();
      public Vector2VariableSet Vectors = new Vector2VariableSet();
      public ColorVariableSet Colors = new ColorVariableSet();
      public TextureVariableSet Textures = new TextureVariableSet();

      public bool AnyDefinition => All.Any(x => x.AnyDefinition);

      public VariableSet[] All => new VariableSet[] {Colors, Vectors, Numbers, Textures};

      public VariableScope Merge(VariableScope other)
      {
         var next = new VariableScope
         {
            Numbers = Numbers.Merge(other.Numbers),
            Vectors = Vectors.Merge(other.Vectors),
            Colors = Colors.Merge(other.Colors),
            Textures = Textures.Merge(other.Textures)
         };
         return next;
      }

      public VariableScope Clone()
      {
         return new VariableScope
         {
            Numbers = Numbers,
            Colors = Colors,
            Vectors = Vectors,
            Textures = Textures
         };
      }

      public VariableSet GetVariableSet<T>()
      {
         return All.FirstOrDefault(x => x.VariableType.IsAssignableFrom(typeof(T)));
      }

      public T Resolve<T>(string variable)
      {
         var variableSet = GetVariableSet<T>();
         return variableSet == null ? default : variableSet.GetGeneric<T>(variable);
      }

      public bool TryResolve<T>(string variable, out T result)
      {
         var variableSet = GetVariableSet<T>();
         return variableSet.TryGetGeneric<T>(variable, out result);
      }

   }

   [System.Serializable]
   [VariableTypeName("Number")]
   public class NumberVariableSet : VariableSet<float> { }

   [System.Serializable]
   [VariableTypeName("Vector2")]
   public class Vector2VariableSet : VariableSet<Vector2> { }

   [System.Serializable]
   [VariableTypeName("Color")]
   public class ColorVariableSet : VariableSet<Color> { }

   [System.Serializable]
   [VariableTypeName("Texture")]
   public class TextureVariableSet : VariableSet<Texture2D> {}

   public abstract class VariableSet
   {
      public abstract bool AnyDefinition { get; }
      public abstract Type VariableType { get; }
      public abstract T GetGeneric<T>(string variable);

      public abstract bool TryGetGeneric<T>(string variable, out T result);

      public abstract void CreateEmpty(string variable);
   }

   [System.Serializable]
   public class VariableSet<T> : VariableSet
   {
      public override bool AnyDefinition => _variableNames.Count > 0;
      public override Type VariableType => typeof(T);
      private T _defaultValue;


      public List<T> _variableValues = new List<T>();
      public List<string> _variableNames = new List<string>();

      public T Get(string variable)
      {
         var index = _variableNames.IndexOf(variable);
         if (index < 0 || index >= _variableValues.Count) return _defaultValue;
         return _variableValues[index];
      }

      public bool TryGet(string variable, out T result)
      {
         var index = _variableNames.IndexOf(variable);
         result = _defaultValue;
         if (index < 0 || index >= _variableValues.Count) return false;
         result = _variableValues[index];
         return true;
      }

      public void Set(string variable, T value)
      {
         var index = _variableNames.IndexOf(variable);
         if (index >=0 )
         {
            _variableValues[index] = value;
         }
         else
         {
            _variableNames.Add(variable);
            _variableValues.Add(value);
         }
      }

      private Dictionary<string, T> CreateDict()
      {
         var output = new Dictionary<string, T>();
         for (var i = 0; i < _variableNames.Count; i++)
         {
            output.Add(_variableNames[i], _variableValues[i]);
         }
         return output;
      }

      public TOther Merge<TOther>(TOther other)
         where TOther : VariableSet<T>, new()
      {
         var nextData = new Dictionary<string, T>();
         foreach (var kvp in CreateDict())
         {
            nextData[kvp.Key] = kvp.Value;
         }

         foreach (var kvp in other.CreateDict())
         {
            nextData[kvp.Key] = kvp.Value;
         }

         var nextNames = new List<string>();
         var nextValues = new List<T>();
         foreach (var kvp in nextData)
         {
            nextNames.Add(kvp.Key);
            nextValues.Add(kvp.Value);
         }

         return new TOther()
         {
            _defaultValue = other._defaultValue,
            _variableNames = nextNames,
            _variableValues = nextValues,
         };
      }

      public override T1 GetGeneric<T1>(string variable)
      {
         if (typeof(T1).IsAssignableFrom(typeof(T)))
         {
            // TODO: revisit this, to see if we can avoid boxing/unboxing the value : (
            return (T1) (object) Get(variable);
         }
         else
         {
            return default;
         }
      }

      public override bool TryGetGeneric<T1>(string variable, out T1 result)
      {
         result = default;
         if (typeof(T1).IsAssignableFrom(typeof(T)))
         {
            // TODO: revisit this, to see if we can avoid boxing/unboxing the value : (
            if (TryGet(variable, out var innerResult))
            {
               result = (T1) (object) innerResult;
               return true;
            }

            return false;
         }
         else
         {
            return false;
         }
      }


      public override void CreateEmpty(string variable)
      {
         Set(variable, _defaultValue);
      }
   }

}