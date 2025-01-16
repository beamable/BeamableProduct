using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using Beamable.Common.Dependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Beamable.Server.Api.Stats
{
    public class MicroserviceStatsApi : AbsStatsApi, IMicroserviceStatsApi
    {
        private const string OBJECT_SERVICE = "object/stats";

        public RequestContext Context { get; }

        public MicroserviceStatsApi(IBeamableRequester requester, RequestContext context, IDependencyProvider provider, UserDataCache<Dictionary<string, string>>.FactoryFunction factoryFunction)
           : base(requester, context, provider, factoryFunction)
        {
            Requester = requester;
            Context = context;
        }

        public Promise<string> GetPublicPlayerStat(long userId, string stat)
        {
	        return GetStats("game", "public", "player", userId, new string[] { stat })
		        .Map(res => res.GetValueOrDefault(stat));
        }

        public Promise<Dictionary<string, string>> GetPublicPlayerStats(long userId, string[] stats)
        {
	        return GetStats("game", "public", "player", userId, stats);
        }

        public Promise<Dictionary<string, string>> GetPublicPlayerStats(long userId)
        {
	        return GetStats("game", "public", "player", userId, null);
        }

        [Obsolete("Use GetProtectedPlayerStats(long userId) instead")]
        public Promise<string> GetProtectedPlayerStat(long userId, string stat)
        {
            return GetStats("game", "private", "player", userId, new string[]
               {
               stat
               }
               ).Map(res => res.GetValueOrDefault(stat));
        }

        public Promise<Dictionary<string, string>> GetProtectedPlayerStats(long userId, string[] stats)
        {
            return GetStats("game", "private", "player", userId, stats);
        }
        
        public Promise<Dictionary<string, string>> GetProtectedPlayerStats(long userId)
        {
	        return GetStats("game", "private", "player", userId, null);
        }

        public Promise<Dictionary<string, string>> GetAllProtectedPlayerStats(long userId)
        {
            return GetStats("game", "private", "player", userId, null);
        }

        public Promise<EmptyResponse> SetProtectedPlayerStat(long userId, string key, string value)
        {
            return SetStats("game", "private", "player", userId, new Dictionary<string, string>
            {
               {key, value}
            });
        }

        public Promise<EmptyResponse> SetProtectedPlayerStats(long userId, Dictionary<string, string> stats)
        {
            return SetStats("game", "private", "player", userId, stats);
        }

        public Promise<EmptyResponse> SetStats(string domain, string access, string type, long userId, Dictionary<string, string> stats)
        {
            var key = $"{domain}.{access}.{type}.{userId}";
            return Requester.Request<EmptyResponse>(Method.POST, $"{OBJECT_SERVICE}/{key}", new StatUpdates(stats));
        }

        public Promise<Dictionary<string, string>> GetStats(string domain, string access, string type, long userId,
           string[] stats)
        {
            var key = $"{domain}.{access}.{type}.{userId}";
            var statString = stats == null ? string.Empty : string.Join(",", stats);
            return Requester.Request<StatsResponse>(Method.GET, $"{OBJECT_SERVICE}/{key}", new StatsRequest
            {
                stats = statString
            }).Map(res =>
            {
                Dictionary<string, string> stats = res.stats.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    if (kvp.Value is JContainer jarray)
                    {
                        return jarray.ToString(Formatting.None);
                    }
                    return $"{kvp.Value}";
                });
                return stats;
            });
        }

        public Promise DeleteProtectedPlayerStats(long userId, string[] stats) => 
	        DeleteStats("game", "private", "player", userId, stats);

        public Promise DeleteStats(string domain, string access, string type, long userId, string[] stats)
        {
	        var key = $"{domain}.{access}.{type}.{userId}";
	        var statString = stats == null ? string.Empty : string.Join(",", stats);
	        return Requester.Request<Unit>(Method.DELETE, $"{OBJECT_SERVICE}/{key}", new StatsRequest
	        {
		        stats = statString
	        }).ToPromise();
        }

        protected override Promise<Dictionary<long, Dictionary<string, string>>> Resolve(string prefix, List<long> gamerTags)
        {
            string queryString = "";
            for (int i = 0; i < gamerTags.Count; i++)
            {
                if (i > 0)
                {
                    queryString += ",";
                }
                queryString += $"{prefix}{gamerTags[i]}";
            }

            return Requester.Request<BatchReadStatsResponse>(
               Method.GET,
               $"/basic/stats/client/batch?format=stringlist&objectIds={queryString}",
               useCache: true
            ).Map(rsp => rsp.ToDictionary());
        }

    }

#pragma warning disable 0649
    class StatsResponse
    {


        public long id;
        public Dictionary<string, object> stats;
    }

    class StatsRequest
    {
        public string stats;
    }


    class StatUpdates
    {
        public Dictionary<string, string> set;

        public StatUpdates(Dictionary<string, string> stats)
        {
            set = stats;
        }
    }
#pragma warning restore 0649

}
