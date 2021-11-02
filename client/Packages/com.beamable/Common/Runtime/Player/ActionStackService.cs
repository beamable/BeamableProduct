using System;
using System.Collections.Generic;

namespace Beamable.Common.Player
{
   public interface ISDKActionStackService
   {
      void Add(ISDKAction action);
   }

   public interface ISDKAction
   {
      SDKActionState State { get; }
      void Predict();
      Promise Execute();
      void Reconcile();
   }

   public abstract class AbsSDKAction : ISDKAction
   {
      public SDKActionState State { get; private set; }

      private Promise _executionPromise;

      public void Predict()
      {
         if (State != SDKActionState.PENDING) return;
         State = SDKActionState.PREDICTED;
         OnPredict();
      }

      public Promise Execute()
      {
         if (State != SDKActionState.PREDICTED) return _executionPromise;
         State = SDKActionState.EXECUTED;
         _executionPromise = OnExecute();
         _executionPromise.Then(_ => Reconcile());
         _executionPromise.Error(_ => Reconcile());
         return _executionPromise;
      }

      public void Reconcile()
      {
         if (State != SDKActionState.EXECUTED) return;
         State = SDKActionState.RECONCILED;
         OnReconcile();
      }

      protected abstract void OnPredict();
      protected abstract Promise OnExecute();
      protected abstract void OnReconcile();
   }

   public enum SDKActionState
   {
      PENDING, PREDICTED, EXECUTED, RECONCILED
   }


   [Serializable]
   public class ActionStackService : ISDKActionStackService
   {
      // TODO: need polymorphic serialization to be viable as an offline mode backup
      private List<ISDKAction> _actions = new List<ISDKAction>();

      public void Add(ISDKAction action)
      {
         _actions.Add(action);
         action.Predict();

         void Clean()
         {
            _actions.Remove(action);
         }

         action.Execute()
            .Then(_ => Clean())
            .Error(_ => Clean());
      }

   }
}