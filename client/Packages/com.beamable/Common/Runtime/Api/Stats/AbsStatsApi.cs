using System.Collections.Generic;

namespace Beamable.Common.Api.Stats
{
   public abstract class AbsStatsApi : IStatsApi
   {
      private readonly UserDataCache<Dictionary<string, string>>.FactoryFunction _cacheFactory;
      public IBeamableRequester Requester { get; }
      public IUserContext UserContext { get; }
      private static long TTL_MS = 15 * 60 * 1000;
      private Dictionary<string, UserDataCache<Dictionary<string, string>>> caches = new Dictionary<string, UserDataCache<Dictionary<string, string>>>();

      public AbsStatsApi(IBeamableRequester requester, IUserContext userContext, UserDataCache<Dictionary<string, string>>.FactoryFunction cacheFactory)
      {
         _cacheFactory = cacheFactory;
         Requester = requester;
         UserContext = userContext;
      }

      public UserDataCache<Dictionary<string, string>> GetCache(string prefix)
      {
         if (!caches.TryGetValue(prefix, out var cache))
         {
            cache = _cacheFactory(
               $"Stats.{prefix}",
               TTL_MS,
               (gamerTags => Resolve(prefix, gamerTags))
            );
            caches.Add(prefix, cache);
         }

         return cache;
      }

      public Promise<EmptyResponse> SetStats (string access, Dictionary<string, string> stats) {
         long gamerTag = UserContext.UserId;
         string prefix = $"client.{access}.player.";
         return Requester.Request<EmptyResponse>(
            Method.POST,
            $"/object/stats/{prefix}{gamerTag}/client/stringlist",
            new StatUpdates(stats)
         ).Then(_ => GetCache(prefix).Remove(gamerTag));
      }

      public Promise<Dictionary<string, string>> GetStats (string domain, string access, string type, long id)
      {
         string prefix = $"{domain}.{access}.{type}.";
         return GetCache(prefix).Get(id);
      }

      protected abstract Promise<Dictionary<long, Dictionary<string, string>>> Resolve(string prefix,
         List<long> gamerTags);
//      {
//         string queryString = "";
//         for (int i = 0; i < gamerTags.Count; i++)
//         {
//            if (i > 0)
//            {
//               queryString += ",";
//            }
//            queryString += $"{prefix}{gamerTags[i]}";
//         }
//         return Requester.Request<BatchReadStatsResponse>(
//            Method.GET,
//            $"/basic/stats/client/batch?format=stringlist&objectIds={queryString}",
//            useCache: true
//         ).RecoverWith(ex =>
//            {
//               return OfflineCache.RecoverDictionary<Dictionary<string, string>>(ex, "stats", Requester.AccessToken, gamerTags).Map(
//                  stats =>
//                  {
//                     var results = stats.Select(kvp =>
//                     {
//                        return new BatchReadEntry
//                        {
//                           id = kvp.Key,
//                           stats = kvp.Value.Select(statKvp => new StatEntry
//                           {
//                              k = statKvp.Key,
//                              v = statKvp.Value
//                           }).ToList()
//                        };
//                     }).ToList();
//
//                     var rsp = new BatchReadStatsResponse
//                     {
//                        results = results
//                     };
//                     return rsp;
//                  });
//               /*
//                * Handle the NoNetworkConnectivity error, by using a custom cache layer.
//                *
//                * the "stats" key cache maintains stats for all users, not per request.
//                */
//
//            })
//            .Map(rsp => rsp.ToDictionary())
//            .Then(playerStats =>
//            {
//               /*
//                * Successfully looked up stats. Commit them to the offline cache.
//                *
//                */
//               OfflineCache.Merge("stats", _requester.AccessToken, playerStats);
//            });
//      }
   }
}