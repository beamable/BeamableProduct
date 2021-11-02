namespace Beamable.Common.Player
{
   public interface ISDKActionStackService
   {
      void Add(ISDKAction action);
   }

   public interface ISDKAction
   {
      void Predict();
      Promise Execute();
      void Reconcile();
   }


   public class ActionStackService
   {

   }
}