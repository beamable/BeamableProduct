//using System.Collections;
//using System.Collections.Generic;
//using System;
//using Beamable.Common;
//using Beamable.Coroutines;
//using Beamable.Service;
//
//namespace Beamable.Platform.SDK {
//   public class UserDataCache<T>
//   {
//      public delegate Promise<Dictionary<long, T>> CacheResolver(List<long> gamerTags);
//
//      private Dictionary<long, UserDataCacheEntry> cache = new Dictionary<long, UserDataCacheEntry>();
//      private long ttlMs = 0;
//
//      private List<long> gamerTagsPending = new List<long>();
//      private List<long> gamerTagsInFlight = new List<long>();
//      private Promise<Dictionary<long, T>> nextPromise = new Promise<Dictionary<long, T>>();
//      private Dictionary<long, T> result = new Dictionary<long, T>();
//      private CacheResolver resolver;
//      private string coroutineContext;
//
//      // If TTL is 0, then never expire anything
//      public UserDataCache(string name, long ttlMs, CacheResolver resolver) {
//         this.ttlMs = ttlMs;
//         this.resolver = resolver;
//         this.coroutineContext = $"userdatacache_{name}";
//      }
//
//      public Promise<T> Get(long gamerTag)
//      {
//         if (gamerTagsPending.Count == 0)
//         {
//            ServiceManager.Resolve<CoroutineService>().StartNew(coroutineContext, ScheduleResolve());
//         }
//         gamerTagsPending.Add(gamerTag);
//         return nextPromise.Map(rsp => rsp[gamerTag]);
//      }
//
//      public Promise<Dictionary<long, T>> GetBatch (List<long> gamerTags)
//      {
//         if (gamerTagsPending.Count == 0)
//         {
//            ServiceManager.Resolve<CoroutineService>().StartNew(coroutineContext, ScheduleResolve());
//         }
//         gamerTagsPending.AddRange(gamerTags);
//         return nextPromise;
//      }
//
//      private IEnumerator ScheduleResolve()
//      {
//         yield return Yielders.EndOfFrame;
//         while (gamerTagsInFlight.Count != 0)
//         {
//            yield return Yielders.EndOfFrame;
//         }
//
//         // Save in flight state and reset pending state
//         var promise = nextPromise;
//         nextPromise = new Promise<Dictionary<long, T>>();
//         result.Clear();
//         gamerTagsInFlight.Clear();
//
//         // Resolve cache
//         for (int i = 0; i < gamerTagsPending.Count; i++)
//         {
//            UserDataCacheEntry found;
//            long nextGamerTag = gamerTagsPending[i];
//            if (result.ContainsKey(nextGamerTag))
//            {
//               continue;
//            }
//
//            if (cache.TryGetValue(nextGamerTag, out found))
//            {
//               if (found.IsExpired(ttlMs))
//               {
//                  cache.Remove(nextGamerTag);
//                  gamerTagsInFlight.Add(nextGamerTag);
//               }
//               else
//               {
//                  result.Add(nextGamerTag, found.data);
//               }
//            }
//            else
//            {
//               if (!gamerTagsInFlight.Contains(nextGamerTag))
//               {
//                  gamerTagsInFlight.Add(nextGamerTag);
//               }
//            }
//         }
//         gamerTagsPending.Clear();
//
//         // Short circuit if cache deflected everything
//         if (gamerTagsInFlight.Count == 0)
//         {
//            promise.CompleteSuccess(result);
//         }
//         else
//         {
//            var resolvedData = resolver.Invoke(gamerTagsInFlight);
//            resolvedData.Then(data =>
//            {
//               gamerTagsInFlight.Clear();
//
//               // Update cache and fill result
//               foreach (var next in data)
//               {
//                  Set(next.Key, next.Value);
//                  result.Add(next.Key, next.Value);
//               }
//
//               // Resolve waiters
//               promise.CompleteSuccess(result);
//            }).Error(err =>
//            {
//               gamerTagsInFlight.Clear();
//               promise.CompleteError(err);
//            });
//         }
//      }
//
//      public void Set (long gamerTag, T data) {
//         cache[gamerTag] = new UserDataCacheEntry(data);
//      }
//
//      public void Remove (long gamerTag) {
//         cache.Remove(gamerTag);
//      }
//
//      private class UserDataCacheEntry {
//         public T data;
//         private long cacheTime;
//
//         public UserDataCacheEntry(T data) {
//            this.data = data;
//            this.cacheTime = Environment.TickCount;
//         }
//
//         public bool IsExpired (long ttlMs) {
//            if (ttlMs == 0) {
//               return false;
//            }
//            return ((Environment.TickCount - cacheTime) > ttlMs);
//         }
//      }
//   }
//}