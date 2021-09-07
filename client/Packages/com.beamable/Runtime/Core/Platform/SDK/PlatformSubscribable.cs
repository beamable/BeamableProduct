//#define PLATFORM_SUBSCRIBABLE_RETRIES_TEST

using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Coroutines;
using Beamable.Service;
using Beamable.Spew;
using UnityEngine;

namespace Beamable.Api
{
   // put constants in a separate class so that they are shared across generic params

   /// <summary>
   /// This type defines the constants of %Subscribables.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See Beamable.API script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   internal static class SubscribableConsts
   {
      internal static readonly int[] RETRY_DELAYS = new int[] {1, 2, 5, 10, 20};
   }

   /// <summary>
   /// This type defines the subscribability of %services.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See Beamable.API script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   /// <typeparam name="TPlatformSubscriber"></typeparam>
   /// <typeparam name="ScopedRsp"></typeparam>
   /// <typeparam name="Data"></typeparam>
   public interface IHasPlatformSubscriber<TPlatformSubscriber, ScopedRsp, Data>
      where TPlatformSubscriber : PlatformSubscribable<ScopedRsp, Data>
   {
      /// <summary>
      /// Allows scopes to consume fresh data when available.
      /// </summary>
      TPlatformSubscriber Subscribable { get; }
   }

   public interface IHasPlatformSubscribers<TPlatformSubscriber, ScopedRsp, Data>
      where TPlatformSubscriber : PlatformSubscribable<ScopedRsp, Data>
   {
      /// <summary>
      /// Allows scopes to consume fresh data when available.
      /// </summary>
      Dictionary<string, TPlatformSubscriber> Subscribables { get; }
   }

   /// <summary>
   /// This type defines the subscribability of %services.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See Beamable.API script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   public abstract class PlatformSubscribable<ScopedRsp, Data> : ISupportsGet<Data>, ISupportGetLatest<Data>
   {
      protected IPlatformService platform;
      protected IBeamableRequester requester;
      protected BeamableGetApiResource<ScopedRsp> getter;
      private string service;
      private Dictionary<string, Data> scopedData = new Dictionary<string, Data>();

      private Dictionary<string, List<PlatformSubscription<Data>>> scopedSubscriptions =
         new Dictionary<string, List<PlatformSubscription<Data>>>();

      private Dictionary<string, ScheduledRefresh> scheduledRefreshes = new Dictionary<string, ScheduledRefresh>();
      private List<string> nextRefreshScopes = new List<string>();
      private Promise<Unit> nextRefreshPromise = null;

      private int retry = 0;

      public bool UsesHierarchyScopes { get; protected set; }

      protected PlatformSubscribable(IPlatformService platform, IBeamableRequester requester, string service, BeamableGetApiResource<ScopedRsp> getter=null)
      {
         if (getter == null)
         {
            getter = new BeamableGetApiResource<ScopedRsp>();
         }

         this.getter = getter;
         this.platform = platform;
         this.requester = requester;
         this.service = service;
         platform.Notification.Subscribe(String.Format("{0}.refresh", service), OnRefreshNtf);

         platform.OnReady.Then(_ => { platform.TimeOverrideChanged += OnTimeOverride; });

         platform.OnShutdown += () => { platform.TimeOverrideChanged -= OnTimeOverride; };

         platform.OnReloadUser += () =>
         {
            Reset();
            Refresh();
         };
      }

      private void OnTimeOverride()
      {
         Refresh();
      }

      protected virtual void Reset()
      {
         // implementation specific clean up code...
      }

      /// <summary>
      /// Subscribe to the callback to receive fresh data when available.
      /// </summary>
      /// <param name="callback"></param>
      /// <returns></returns>
      public PlatformSubscription<Data> Subscribe(Action<Data> callback)
      {
         return Subscribe("", callback);
      }

      /// <summary>
      /// Subscribe to the callback to receive fresh data when available.
      /// </summary>
      /// <param name="scope"></param>
      /// <param name="callback"></param>
      /// <returns></returns>
      public PlatformSubscription<Data> Subscribe(string scope, Action<Data> callback)
      {
         List<PlatformSubscription<Data>> subscriptions;
         if (!scopedSubscriptions.TryGetValue(scope, out subscriptions))
         {
            subscriptions = new List<PlatformSubscription<Data>>();
            scopedSubscriptions.Add(scope, subscriptions);
         }

         var subscription = new PlatformSubscription<Data>(scope, callback, Unsubscribe);
         subscriptions.Add(subscription);

         Data data;
         if (scopedData.TryGetValue(scope, out data))
         {
            callback.Invoke(data);
         }
         else
         {
            // Refresh if this is the first subscription ever
            if (subscriptions.Count == 1)
               Refresh(scope);
            else
            {
               Debug.LogWarning("A subscription has gone silent. " + subscriptions.Count + " " + scope + " " + this.GetType().Name);
            }
         }

         return subscription;
      }

      /// <summary>
      /// Manually refresh the available data.
      /// </summary>
      /// <returns></returns>
      protected Promise<Unit> Refresh()
      {
         return Refresh("");
      }

      private bool ShouldRejectScopeFromRefresh(string scope)
      {
         if (!UsesHierarchyScopes)
         {
            return (!scopedSubscriptions.ContainsKey(scope));
         }

         return !scopedSubscriptions.Any(kvp => scope.StartsWith(kvp.Key));
      }

      /// <summary>
      /// Manually refresh the available data.
      /// </summary>
      /// <param name="scope"></param>
      /// <returns></returns>
      protected Promise<Unit> Refresh(string scope)
      {
         if (scope == "")
         {
            nextRefreshScopes.Clear();
            nextRefreshScopes.AddRange(scopedSubscriptions.Keys);
         }
         else
         {
            if (ShouldRejectScopeFromRefresh(scope))
               return Promise<Unit>.Successful(PromiseBase.Unit);

            if (nextRefreshScopes.Contains(scope) && nextRefreshPromise != null)
               return nextRefreshPromise;


            nextRefreshScopes.Add(scope);
         }

         if (nextRefreshScopes.Count == 0)
            return Promise<Unit>.Successful(PromiseBase.Unit);

         if (nextRefreshPromise == null)
         {
            nextRefreshPromise = new Promise<Unit>();
            Debug.Log("Starting refresh exectuion");
            ServiceManager.Resolve<CoroutineService>().StartCoroutine(ExecuteRefresh());
         }

         return nextRefreshPromise;
      }

      private IEnumerator ExecuteRefresh()
      {
         Debug.Log("executing refresh " + GetType().Name);

         yield return Yielders.EndOfFrame; // <-- this line is breaking everything. Its never coming back.
         Debug.Log("executing refresh2 " + GetType().Name);

         var promise = nextRefreshPromise;
         nextRefreshPromise = null;
         var scope = string.Join(",", nextRefreshScopes);
         nextRefreshScopes.Clear();

         ExecuteRequest(requester, CreateRefreshUrl(scope)).Error(err =>
         {
            Debug.Log("executing refresh3 error " + GetType().Name + " " + err.Message);

            var delay = SubscribableConsts.RETRY_DELAYS[Math.Min(retry, SubscribableConsts.RETRY_DELAYS.Length - 1)];
            PlatformLogger.Log($"PLATFORM SUBSCRIBABLE: Error {service}:{scope}:{err}; Retry {retry + 1} in {delay}");
            promise.CompleteError(err);
            var scopes = scope.Split(',');
            if (scopes.Length > 0)
            {
               // Collapse all outstanding scopes into the next refresh
               for (int i = 0; i < scopes.Length; i++)
               {
                  if (!nextRefreshScopes.Contains(scopes[i]))
                     nextRefreshScopes.Add(scopes[i]);
               }

               // Schedule a refresh delay to capture all outstanding scopes
               ScheduleRefresh(delay, scopes[0]);
            }
            else
            {
               ScheduleRefresh(delay, "");
            }

            // Avoid incrementing the backoff if the device is definitely not connected to the network at all.
            // This is narrow, and would still increment if the device is connected, but the internet has other problems
            if (platform.ConnectivityService.HasConnectivity)
            {
               retry += 1;
            }
         }).Then(OnRefresh).Then(_ =>
         {
            retry = 0;
            promise.CompleteSuccess(PromiseBase.Unit);
         });
      }

      protected virtual Promise<ScopedRsp> ExecuteRequest(IBeamableRequester requester, string url)
      {
         return getter.RequestData(requester, url);
      }

      protected virtual string CreateRefreshUrl(string scope)
      {
         return getter.CreateRefreshUrl(platform, service, scope);
      }

      /// <summary>
      /// Manually fetch the available data.
      /// </summary>
      /// <returns></returns>
      public Data GetLatest()
      {
         return GetLatest("");
      }

      /// <summary>
      /// Manually fetch the available data.
      /// </summary>
      /// <param name="scope"></param>
      /// <returns></returns>
      public Data GetLatest(string scope)
      {
         Data data;
         scopedData.TryGetValue(scope, out data);
         return data;
      }

      /// <summary>
      /// Manually fetch the available data.
      /// </summary>
      /// <param name="scope"></param>
      /// <returns></returns>
      public Promise<Data> GetCurrent(string scope = "")
      {
         if (scopedData.TryGetValue(scope, out var data))
         {
            return Promise<Data>.Successful(data);
         }
         else
         {
            var promise = new Promise<Data>();
            // TODO: Introduce a shared promise that wraps the last refresh, so we don't make lots of needless promise instances
            var subscription = Subscribe(scope, nextData =>
            {
               promise.CompleteSuccess(nextData);
            });

            return promise.Then(_ => subscription.Unsubscribe());
         }
      }

      /// <summary>
      /// Manually notify observing scopes regarding the available data.
      /// </summary>
      /// <param name="data"></param>
      public void Notify(Data data)
      {
         Notify("", data);
      }

      /// <summary>
      /// Manually notify observing scopes regarding the available data.
      /// </summary>
      /// <param name="scope"></param>
      /// <param name="data"></param>
      public void Notify(string scope, Data data)
      {
         List<PlatformSubscription<Data>> subscriptions;
         if (scopedSubscriptions.TryGetValue(scope, out subscriptions))
         {
            scopedData[scope] = data;

            for (var i = subscriptions.Count; i > 0; i--)
            {
               subscriptions[i - 1].Invoke(data);
            }
         }
      }

      protected abstract void OnRefresh(ScopedRsp data);

      private void OnRefreshNtf(object payloadRaw)
      {
         var payload = payloadRaw as IDictionary<string, object>;

         List<string> scopes = new List<string>();
         object delayRaw = null;
         object scopesRaw = null;
         int delay = 0;

         if (payload != null)
         {
            if (payload.TryGetValue("scopes", out scopesRaw))
            {
               List<object> scopesListRaw = (List<object>) scopesRaw;
               foreach (var next in scopesListRaw)
               {
                  scopes.Add(next.ToString());
               }
            }

            if (payload.TryGetValue("delay", out delayRaw))
            {
               delay = int.Parse(delayRaw.ToString());
            }
            else
            {
               delay = 0;
            }
         }

         if (scopes.Count == 0)
         {
            foreach (var scope in scopedSubscriptions.Keys)
            {
               scopes.Add(scope);
            }
         }


         if (delay == 0)
         {
            foreach (var scope in scopes)
            {
               Refresh(scope);
            }
         }
         else
         {
            foreach (var scope in scopes)
            {
               int jitterDelay = UnityEngine.Random.Range(0, delay);
               ScheduleRefresh(jitterDelay, scope);
            }
         }
      }

      protected void ScheduleRefresh(long seconds, string scope)
      {
         DateTime refreshTime = DateTime.UtcNow.AddSeconds(seconds);
         var coroutineService = ServiceManager.Resolve<CoroutineService>();
         ScheduledRefresh current;
         if (scheduledRefreshes.TryGetValue(scope, out current))
         {
            // If the existing refresh time is sooner, ignore this scheduled refresh
            if (current.refreshTime.CompareTo(refreshTime) <= 0)
            {
               PlatformLogger.Log(
                  $"PLATFORM SUBSCRIBABLE: Ignoring refresh for {service}:{scope}; there is a sooner refresh");
               return;
            }

            coroutineService.StopCoroutine(current.coroutine);
            scheduledRefreshes.Remove(scope);
         }

         var coroutine = coroutineService.StartCoroutine(RefreshIn(seconds, scope));
         scheduledRefreshes.Add(scope, new ScheduledRefresh(coroutine, refreshTime));
      }

      private IEnumerator RefreshIn(long seconds, string scope)
      {
         PlatformLogger.Log($"PLATFORM SUBSCRIBABLE: Schedule {service}:{scope} in {seconds}");
         yield return new WaitForSecondsRealtime(seconds);
         Refresh(scope);
         scheduledRefreshes.Remove(scope);
      }

      protected void Unsubscribe(string scope, PlatformSubscription<Data> subscription)
      {
         List<PlatformSubscription<Data>> subscriptions;
         if (scopedSubscriptions.TryGetValue(scope, out subscriptions))
         {
            subscriptions.Remove(subscription);
            if (subscriptions.Count == 0)
            {
               // FIXME(?): should this also cancel any scheduled refreshes for this scope?
               scopedSubscriptions.Remove(scope);
               scopedData.Remove(scope);
            }
         }
      }
   }

   // A class instead of a struct to reduce code-size bloat from the generic dictionary instantiation
   class ScheduledRefresh
   {
      public Coroutine coroutine;
      public DateTime refreshTime;

      public ScheduledRefresh(Coroutine coroutine, DateTime refreshTime)
      {
         this.coroutine = coroutine;
         this.refreshTime = refreshTime;
      }
   }

   public class PlatformSubscription<T>
   {
      private Action<T> callback;
      private string scope;
      private Action<string, PlatformSubscription<T>> onUnsubscribe;

      public string Scope => scope;

      internal PlatformSubscription(string scope, Action<T> callback,
         Action<string, PlatformSubscription<T>> onUnsubscribe)
      {
         this.scope = scope;
         this.callback = callback;
         this.onUnsubscribe = onUnsubscribe;
      }

      internal void Invoke(T data)
      {
         callback.Invoke(data);
      }

      public void Unsubscribe()
      {
         onUnsubscribe.Invoke(scope, this);
      }
   }
}

namespace Beamable
{
   public static class PlatformSubscribableExtensions
   {
      public static PlatformSubscription<TData> Subscribe<TPlatformSubscribable, TScopedRsp, TData>(
         this IHasPlatformSubscriber<TPlatformSubscribable, TScopedRsp, TData> subscribable,
         Action<TData> callback)

         where TPlatformSubscribable : PlatformSubscribable<TScopedRsp, TData>
      {
         return subscribable.Subscribable.Subscribe(callback);
      }

      public static PlatformSubscription<TData> Subscribe<TPlatformSubscribable, TScopedRsp, TData>(
         this IHasPlatformSubscriber<TPlatformSubscribable, TScopedRsp, TData> subscribable,
         string scopes,
         Action<TData> callback)

         where TPlatformSubscribable : PlatformSubscribable<TScopedRsp, TData>
      {
         return subscribable.Subscribable.Subscribe(scopes, callback);
      }

      public static TData GetLatest<TPlatformSubscribable, TScopedRsp, TData>(
         this IHasPlatformSubscriber<TPlatformSubscribable, TScopedRsp, TData> subscribable,
         string scopes="") where TPlatformSubscribable : PlatformSubscribable<TScopedRsp, TData>
      {
         return subscribable.Subscribable.GetLatest(scopes);
      }
   }
}