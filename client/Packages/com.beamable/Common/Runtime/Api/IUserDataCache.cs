using System;
using System.Collections.Generic;

namespace Beamable.Common.Api
{
   public abstract class UserDataCache<T>
   {
      public delegate UserDataCache<T> FactoryFunction(string name, long ttlMs, CacheResolver resolver);

      public delegate Promise<Dictionary<long, T>> CacheResolver(List<long> gamerTags);

      public abstract Promise<T> Get(long gamerTag);
      public abstract Promise<Dictionary<long, T>> GetBatch(List<long> gamerTags);
      public abstract void Set(long gamerTag, T data);
      public abstract void Remove(long gamerTag);

      protected class UserDataCacheEntry {
         public T data;
         private long cacheTime;

         public UserDataCacheEntry(T data) {
            this.data = data;
            this.cacheTime = Environment.TickCount;
         }

         public bool IsExpired (long ttlMs) {
            if (ttlMs == 0) {
               return false;
            }
            return ((Environment.TickCount - cacheTime) > ttlMs);
         }
      }
   }
}