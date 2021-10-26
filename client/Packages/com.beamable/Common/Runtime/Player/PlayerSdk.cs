using System;
using System.Collections.Generic;
using Beamable.Common.Api;

namespace Beamable.Common.Player
{

   public class PlayerSdk
   {
      public IPlayerWidgets Widgets;
      public IPlayerStats PublicStats;
      public IPlayerStats PrivateStats;

      public PlayerSdk(IPlayerWidgets widgets, IPlayerStats stats)
      {
         Widgets = widgets;
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


   public abstract class DefaultObservable<T> : IObservable<T>
      where T : new()
   {
      public bool IsLoading { get; protected set; }
      public IBeamableRequester Requester { get; }

      public T Data { get; private set; } = new T();
      public event Action<T> OnUpdated;
      public event Action OnLoadingStarted;
      public event Action OnLoadingFinished;

      protected ActionStack _actionStack = new ActionStack();

      private Promise<T> _pendingRefreshPromise;

      public DefaultObservable(IBeamableRequester requester)
      {
         Requester = requester;
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

      private void ConfigureAction(ISdkAction action)
      {
         // apply userId and other configuration tooling.
         action.Requester = Requester; // The request should be configured per user.
      }


      public void Update()
      {
         // meant to be called on the game update loop.
         _actionStack.EvaluateActionStack();
      }

      protected abstract Promise<T> PerformFetch();
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

   public interface IActionStackMiddleware
   {
      ISdkAction OnPush(ISdkAction newAction);
      ISdkAction OnPop(ISdkAction poppedAction);
   }

   public class DefaultActionStackMiddleware : IActionStackMiddleware
   {
      public virtual ISdkAction OnPush(ISdkAction newAction)
      {
         return newAction;
      }

      public virtual ISdkAction OnPop(ISdkAction poppedAction)
      {
         return poppedAction;
      }
   }

   public class ActionStack
   {
      private Stack<ISdkAction> _stack;

      private List<IActionStackMiddleware> _middleware = new List<IActionStackMiddleware>();

      public ActionStack()
      {
         _stack = new Stack<ISdkAction>();
      }

      public int Count => _stack.Count;
      public bool IsEmpty => Count == 0;

      private Dictionary<ISdkAction, Promise> _actionToResult = new Dictionary<ISdkAction, Promise>();

      public void RegisterMiddleware(IActionStackMiddleware middleware)
      {
         _middleware.Add(middleware);
      }

      public Promise Push(ISdkAction action)
      {
         foreach (var middleware in _middleware)
         {
            action = middleware?.OnPush(action);
         }
         _stack.Push(action);
         return _actionToResult[action] = new Promise();
      }

      public bool TryPop(out ISdkAction action)
      {
         action = null;
         if (IsEmpty) return false;

         action = _stack.Pop();
         foreach (var middleware in _middleware)
         {
            action = middleware.OnPop(action);
         }
         return true;
      }

      public void EvaluateActionStack()
      {
         if (!TryPop(out var action)) return;

         var promise = action.Execute();
         promise.Merge(_actionToResult[action]);
      }

      // TODO: if offline, use appropriate methods
      // TODO: if unity editor, keep history of actions for diagnostics logging
   }

   public class PlayerWidget
   {

   }


   public interface IPlayerWidgets : IObservable<List<PlayerWidget>>
   {
      Promise Add(PlayerWidget widget);
      Promise Remove(PlayerWidget widget);

   }

   public class PlayerWidgets : DefaultObservable<List<PlayerWidget>> , IPlayerWidgets
   {

      public PlayerWidgets(IBeamableRequester requester) : base(requester)
      {
      }

      protected override Promise<List<PlayerWidget>> PerformFetch()
      {
         throw new NotImplementedException("");
      }

      public Promise Add(PlayerWidget widget)
      {
         return PushAction(new AddWidgetAction(this, widget));
      }

      public Promise Remove(PlayerWidget widget)
      {
         throw new NotImplementedException();
      }
   }

   public class AddWidgetAction : UserSdkAction
   {
      private readonly IPlayerWidgets _collection;
      private readonly PlayerWidget _widget;

      public AddWidgetAction(IPlayerWidgets collection, PlayerWidget widget)
      {
         _collection = collection;
         _widget = widget;
      }

      public override Promise Execute()
      {
         return Requester.Request<EmptyResponse>(Method.POST, "derp", _widget).Then(_ =>
         {
            // reconcile output.
            _collection.Data.Add(_widget);
         }).ToPromise();
      }
   }
}