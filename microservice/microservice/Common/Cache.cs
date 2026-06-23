using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace microservice.Common
{
   public class Cache<TKey, TValue>
   {
      public delegate Task<TValue> Resolver(TKey key);

      private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _data = new();
      private readonly Resolver _resolver;

      public Cache(Resolver resolver)
      {
         _resolver = resolver;
      }

      public void PurgeAllExcept(HashSet<TKey> keysToKeep)
      {
         var existingKeys = _data.Keys.ToHashSet();
         var keysToRemove = existingKeys.Except(keysToKeep);

         foreach (var key in keysToRemove)
         {
            Purge(key);
         }
      }

      public void PurgeAll()
      {
         _data.Clear();
      }

      public void Purge(TKey key)
      {
         _data.TryRemove(key, out _);
      }

      /// <summary>
      /// Shares an in-flight resolver task and caches successful results. Faulted or canceled
      /// tasks are removed so a later request can retry the resolver.
      /// </summary>
      public Task<TValue> Get(TKey key)
      {
         Lazy<Task<TValue>> candidate = null;
         candidate = new Lazy<Task<TValue>>(
            () => ResolveAndEvictOnFailure(key, candidate),
            LazyThreadSafetyMode.ExecutionAndPublication);

         var entry = _data.GetOrAdd(key, candidate);
         return entry.Value;
      }

      private async Task<TValue> ResolveAndEvictOnFailure(TKey key, Lazy<Task<TValue>> entry)
      {
         try
         {
            return await _resolver(key);
         }
         catch
         {
            // Remove only this failed entry. A newer value may have replaced it after a purge.
            ((ICollection<KeyValuePair<TKey, Lazy<Task<TValue>>>>)_data)
               .Remove(new KeyValuePair<TKey, Lazy<Task<TValue>>>(key, entry));
            throw;
         }
      }
   }
}
