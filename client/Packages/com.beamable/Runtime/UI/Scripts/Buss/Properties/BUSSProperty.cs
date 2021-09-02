namespace Beamable.UI.Buss.Properties
{
   public abstract class BUSSProperty
   {
      public bool Enabled;
      public bool HasAnyStyles => Enabled && AnyDefinition;
      protected abstract bool AnyDefinition { get; }
   }

   public static class BUSSPropertyExtensions
   {
      public static bool IsDefined(this BUSSProperty self)
      {
         return self != null && self.Enabled;
      }
   }

   public interface IBUSSProperty<T> where T : BUSSProperty
   {
      T OverrideWith(T other);
      T Clone();
   }
}