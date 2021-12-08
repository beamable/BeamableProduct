using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Stats
{
   public interface IStatsApi
   {
      UserDataCache<Dictionary<string, string>> GetCache(string prefix);
      Promise<EmptyResponse> SetStats(string access, Dictionary<string, string> stats);
      Promise<Dictionary<string, string>> GetStats(string domain, string access, string type, long id);
   }

   [Serializable]
   public class BatchReadStatsResponse
   {
      public List<BatchReadEntry> results;

      public Dictionary<long, Dictionary<string, string>> ToDictionary () {
         Dictionary<long, Dictionary<string, string>> result = new Dictionary<long, Dictionary<string, string>>();
         foreach (var entry in results)
         {
            result[entry.id] = entry.ToStatsDictionary();
         }
         return result;
      }
   }

   [Serializable]
   public class BatchReadStatsRequest
   {
      public string objectIds;
      public string stats;
      public string format;
   }

   [Serializable]
   public class BatchReadEntry
   {
      public long id;
      public List<StatEntry> stats;

      public Dictionary<string, string> ToStatsDictionary () {
         Dictionary<string, string> result = new Dictionary<string, string>();
         foreach (var stat in stats)
         {
            var value = $"{stat.v}";
            #if DB_MICROSERVICE
            if (stat.v is Newtonsoft.Json.Linq.JContainer jContainer)
            {
               value = jContainer.ToString(Newtonsoft.Json.Formatting.None);
            }
            #endif

            result[stat.k] = value;
         }
         return result;
      }
   }

   [Serializable]
   public class StatEntry
   {
      public string k;
#if DB_MICROSERVICE
      public object v;
#else
      public string v;
#endif
   }

   [Serializable]
   public class StatUpdates
   {
      public List<StatEntry> set;

      public StatUpdates(Dictionary<string, string> stats)
      {
         set = new List<StatEntry>();
         foreach (var stat in stats)
         {
            var entry = new StatEntry {k = stat.Key, v = stat.Value};
            set.Add(entry);
         }
      }
   }
}