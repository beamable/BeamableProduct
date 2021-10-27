using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Player;
using Beamable.Coroutines;
using Beamable.Service;
using UnityEngine;

namespace Beamable.Player
{

   public class WaitForSdkActions : CustomYieldInstruction
   {
      private readonly ActionStack _stack;

      public WaitForSdkActions(ActionStack stack)
      {
         _stack = stack;
      }

      public override bool keepWaiting => _stack.Count == 0;
   }

   public abstract class DefaultObservable<T> : Common.Player.IObservable<T>
      where T : new()
   {
      public bool IsLoading { get; protected set; }
      public IBeamableRequester Requester { get; }

      public T Data { get; private set; } = new T();
      public event Action<T> OnUpdated;
      public event Action OnLoadingStarted;
      public event Action OnLoadingFinished;

      protected ActionStack _actionStack;

      private Promise<T> _pendingRefreshPromise;

      public DefaultObservable(IBeamableRequester requester)
      {
         _actionStack = new ActionStack(GetNextAction);
         Requester = requester;
         ServiceManager.Resolve<CoroutineService>().StartNew("player-sdk-stat", UpdateForever());
      }

      public async Promise Refresh()
      {
         if (IsLoading)
         {
            await _pendingRefreshPromise;
            return;
         }

         IsLoading = true;
         OnLoadingStarted?.Invoke();
         try
         {
            _pendingRefreshPromise = PerformFetch();
            Data = await _pendingRefreshPromise;
            OnUpdated?.Invoke(Data);
         }
         finally
         {
            IsLoading = false;
            _pendingRefreshPromise = null;
            OnLoadingFinished?.Invoke();
         }
      }

      protected Promise PushAction(ISdkAction action)
      {
         ConfigureAction(action);
         return _actionStack.Push(action);
      }

      protected Promise PushRefresh() => PushAction(new RefreshSdkAction(this));

      protected void ConfigureAction(ISdkAction action)
      {
         // apply userId and other configuration tooling.
         action.Requester = Requester; // The request should be configured per user.
      }

      protected IEnumerator UpdateForever()
      {
         _actionStack.EvaluateActionStack();
         yield return new WaitForSdkActions(_actionStack);
      }

      protected abstract Promise<T> PerformFetch();

      private class RefreshSdkAction : ISdkAction
      {
         private Common.Player.IObservable<T> _observable;

         public RefreshSdkAction(Common.Player.IObservable<T> observable)
         {
            _observable = observable;
         }
         public Promise Execute()
         {
            return _observable.Refresh();
         }
         public IBeamableRequester Requester { get; set; } // TODO not needed.
      }

      public virtual ISdkAction GetNextAction(IEnumerable<ISdkAction> set)
      {
         return set.First();
      }
   }

   public abstract class UserObservable<T> : DefaultObservable<T> ,IUserContext
      where T : new()
   {
      protected UserObservable(long userId, IBeamableRequester requester) : base(requester)
      {
         UserId = userId;
      }

      public long UserId { get; }
   }

}