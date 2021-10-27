using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Api;

namespace Beamable.Common.Player
{

   public class PlayerSdk
   {
      public IPlayerStats PublicStats;
      public IPlayerStats PrivateStats;

      public PlayerSdk(IPlayerStats stats)
      {
         PublicStats = stats;
      }

   }

   public interface IObservable<out T>
   {
      bool IsLoading { get; }
      T Data { get; }
      event Action<T> OnUpdated;
      event Action OnLoadingStarted;
      event Action OnLoadingFinished;
      Promise Refresh();
   }


   public interface ISdkAction
   {
      Promise Execute();
      IBeamableRequester Requester { get; set; }
   }




   public abstract class UserSdkAction : ISdkAction
   {
      public IBeamableRequester Requester { get; set; }

      public abstract Promise Execute();
   }

   public class ActionStack
   {
      private readonly GetNextAction _getter;
      private Stack<ISdkAction> _stack;

      public delegate ISdkAction GetNextAction(IEnumerable<ISdkAction> set);

      public ActionStack(GetNextAction getter=null)
      {
         _getter = getter;
         if (getter == null)
         {
            _getter = set => set.First();
         }
         _stack = new Stack<ISdkAction>();
      }

      public int Count => _stack.Count;
      public bool IsEmpty => Count == 0;

      private Dictionary<ISdkAction, Promise> _actionToResult = new Dictionary<ISdkAction, Promise>();

      public Promise Push(ISdkAction action)
      {
         _stack.Push(action);
         return _actionToResult[action] = new Promise();
      }

      public bool TryPeek(out ISdkAction action)
      {
         action = null;
         if (IsEmpty) return false;
         action = _stack.Peek();
         return true;
      }

      public bool TryPop(out ISdkAction action)
      {
         action = null;
         if (IsEmpty) return false;

         action = _getter(_stack);
         return true;
      }

      public void EvaluateActionStack()
      {
         if (!TryPop(out var action)) return;

         var promise = action.Execute();
         promise.Merge(_actionToResult[action]);
         _actionToResult[action].Then(_ => _actionToResult.Remove(action));
      }

      // TODO: if offline, use appropriate methods
      // TODO: if unity editor, keep history of actions for diagnostics logging
   }
}