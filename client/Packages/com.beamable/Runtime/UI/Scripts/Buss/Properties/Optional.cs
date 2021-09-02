
using UnityEngine;

namespace Beamable.UI.Buss.Properties
{

   public abstract class Optional
   {
      public bool HasValue;

      public string VariableReference = null;

   }
   public class Optional<T> : Optional, ISupportsReference<T>
   {
      public T Value;
      public void Set(bool hasValue, T val)
      {
         Value = val;
         HasValue = hasValue;
      }

      public bool TryGetValue(IVariableScope scope, out T value)
      {
         if (HasValue)
         {
            value = GetValue(scope);
            return true;
         }

         value = default;
         return false;
      }

      public T GetValue(IVariableScope scope)
      {
         if (string.IsNullOrEmpty(VariableReference))
         {
            return HasValue ? Value : default; // there is no variable reference here, so use the hard coded value.
         }
         else
         {
            return scope.Resolve<T>(VariableReference); // lets use the scoped reference
         }
      }

      public static TOptional CreateInstance<TOptional>(T value) where TOptional:Optional<T>, new()
      {
         return new TOptional
         {
            Value = value,
            HasValue = true
         };
      }
      public static TOptional CreateInstance<TOptional>() where TOptional:Optional<T>, new()
      {
         return new TOptional
         {
            HasValue = false
         };
      }

   }

   public static class OptionalExtensions
   {
      public static T Merge<T, U>(this T self, T other) where T : Optional<U>, new()
      {
         var next = new T();

         if (other != null && other.HasValue)
         {
            next.Set(true, other.Value);
         }
         else if (self != null)
         {
            next.Set(self.HasValue, self.Value);
         }
         else
         {
            next.Set(false, default);
         }


         var otherHasVariable = !string.IsNullOrEmpty(other?.VariableReference);
         var selfHasVariable = !string.IsNullOrEmpty(self?.VariableReference);

         //next.VariableReference = self?.VariableReference;

         if (selfHasVariable && self.HasValue && !other.HasValue)
         {
            next.VariableReference = self.VariableReference;
         }
         if (otherHasVariable && next.HasValue)
         {
            //next.HasValue = true;
            next.VariableReference = other.VariableReference;// ?? self.VariableReference;
         }
         //else
         {
           // next.HasValue = true;
         }

         //next.VariableReference = self?.VariableReference ?? other?.VariableReference;
         if (!string.IsNullOrEmpty(next.VariableReference))
         {
            next.HasValue = true;
         }

         return next;
      }

   }


   [System.Serializable]
   public class OptionalColor : Optional<Color> {}

   [System.Serializable]
   public class OptionalTexture : Optional<Texture2D> {}

   [System.Serializable]
   public class OptionalSprite : Optional<Sprite> {}

   [System.Serializable]
   public class OptionalNumber : Optional<float> {}

   [System.Serializable]
   public class OptionalVector2 : Optional<Vector2> {}

   [System.Serializable]
   public class OptionalFontAsset : Optional<TMPro.TMP_FontAsset> {}

   [System.Serializable]
   public class OptionalMaterial : Optional<Material> {}

   [System.Serializable]
   public class OptionalFontStyle : Optional<TMPro.FontStyles> {}

   // NOT SUPPORTED ON WINDOWS
//   [System.Serializable]
//   public class OptionalTextAlignment : Optional<TMPro._HorizontalAlignmentOptions> {}
//
//   [System.Serializable]
//   public class OptionalVerticalAlignment : Optional<TMPro._VerticalAlignmentOptions> {}
}