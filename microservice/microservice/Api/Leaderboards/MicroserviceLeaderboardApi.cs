using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Leaderboards;
using Beamable.Common.Pooling;
using Beamable.Common.Shop;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;


namespace Beamable.Server.Api.Leaderboards
{
    public class MicroserviceLeaderboardApi : LeaderboardApi, IMicroserviceLeaderboardsApi
    {
        private const string SERVICE_PATH = "object/leaderboards";


        public RequestContext Context { get; }

        public MicroserviceLeaderboardApi(IBeamableRequester requester, RequestContext context, IDependencyProvider provider, UserDataCache<RankEntry>.FactoryFunction factoryFunction)
            : base(requester, context, provider, factoryFunction)
        {
            Context = context;
        }

        public Promise<EmptyResponse> CreateLeaderboard(string leaderboardId,
            LeaderboardContent templateLeaderboardContent,
            OptionalLong ttl = null,
            OptionalListString derivatives = null,
            OptionalLong freezeTime = null)
        {
            var maxEntries = templateLeaderboardContent.max_entries;
            var partitioned = templateLeaderboardContent.partitioned;
            var cohorts = templateLeaderboardContent.cohortSettings;
            var permissions = templateLeaderboardContent.permissions;

            var derivativesValue = default(List<string>);
            if (derivatives != null && derivatives.HasValue)
                derivativesValue = derivatives.Value;

            var request = new CreateLeaderboardRequest()
            {
                maxEntries = maxEntries,
                ttl = ttl,
                partitioned = partitioned,
                cohortSettings = cohorts.Value,
                derivatives = derivativesValue,
                permissions = permissions,
                freezeTime = freezeTime
            };

            return CreateLeaderboard(leaderboardId, request);
        }

        public Promise<EmptyResponse> CreateLeaderboard(string leaderboardId,
            OptionalInt maxEntries = null,
            OptionalLong ttl = null,
            OptionalBoolean partitioned = null,
            OptionalCohortSettings cohortSettings = null,
            OptionalListString derivatives = null,
            OptionalClientPermissions permissions = null,
            OptionalLong freezeTime = null)
        {
            var req = new CreateLeaderboardRequest()
            {
                maxEntries = maxEntries ?? new OptionalInt(),
                ttl = ttl ?? new OptionalLong(),
                partitioned = partitioned ?? new OptionalBoolean(),
                cohortSettings = (cohortSettings ?? new OptionalCohortSettings()).Value,
                derivatives = (derivatives ?? new OptionalListString()).Value,
                permissions = (permissions ?? new OptionalClientPermissions()).Value,
                freezeTime = freezeTime ?? new OptionalLong()
            };

            return CreateLeaderboard(leaderboardId, req);
        }

        public Promise<EmptyResponse> CreateLeaderboard(string leaderboardId, CreateLeaderboardRequest req)
        {
            using var pooledBuilder = StringBuilderPool.StaticPool.Spawn();

            var dict = new ArrayDict();

            if (req.maxEntries.HasValue) dict.Add("maxEntries", req.maxEntries.Value);
            if (req.ttl.HasValue) dict.Add("ttl", req.ttl.Value);
            if (req.partitioned.HasValue) dict.Add("partitioned", req.partitioned.Value);

            if (req.cohortSettings != null && req.cohortSettings.cohorts != null && req.cohortSettings.cohorts.Count > 0)
            {
                var propDict = new ArrayDict();
                var cohorts = req.cohortSettings.cohorts;
                propDict.Add("cohorts", cohorts.Select(cohort =>
                {
                    var arrayDict = new ArrayDict();
                    arrayDict.Add("id", cohort.id);

                    if (cohort.description != null && cohort.description.HasValue)
                        arrayDict.Add("description", cohort.description.Value);

                    arrayDict.Add("statRequirements", cohort.statRequirements.Select(delegate (StatRequirement requirement)
                    {
                        var statDict = new ArrayDict();
                        if (requirement.domain != null && requirement.domain.HasValue)
                            statDict.Add("domain", requirement.domain.Value);

                        if (requirement.access != null && requirement.access.HasValue)
                            statDict.Add("access", requirement.access.Value);

                        statDict.Add("constraint", requirement.constraint);
                        statDict.Add("stat", requirement.stat);
                        statDict.Add("value", requirement.value);
                        return statDict;
                    }).Cast<object>().ToArray());

                    return arrayDict;
                }).Cast<object>().ToArray());

                dict.Add("cohortSettings", propDict);
            }

            if (req.derivatives != null && req.derivatives.Count > 0)
            {
                dict.Add("derivatives", req.derivatives.Cast<object>().ToArray());
            }

            if (req.permissions != null)
            {
                dict.Add("permissions", new ArrayDict()
                {
                    {"write_self", req.permissions.writeSelf }
                });
            }

            if (req.freezeTime.HasValue) dict.Add("freezeTime", req.freezeTime.Value);

            var json = Json.Serialize(dict, pooledBuilder.Builder);
            return Requester.Request<EmptyResponse>(Method.POST, $"{SERVICE_PATH}/{leaderboardId}", json);
        }

        public async Promise<ListLeaderboardResult> ListLeaderboards(int? skip = null, int? limit=50)
        {
            using var pooledBuilder = StringBuilderPool.StaticPool.Spawn();

            var req = new ArrayDict();
            if (skip.HasValue)
            {
                req["skip"] = skip;
            }
            if (limit.HasValue)
            {
                req["limit"] = limit;
            }
            var json = Json.Serialize(req, pooledBuilder.Builder);

            var res = await Requester.Request<ListLeaderboardResponse>(
                Method.GET,
                $"/basic/leaderboards/list",
                json
            );
            return new ListLeaderboardResult
            {
                ids = res.nameList,
                offset = res.offset,
                total = res.total
            };
        }

        public Promise<GetPlayerLeaderboardsResponse> GetPlayerLeaderboards(long gamerTag)
        {
            return Requester.Request<GetPlayerLeaderboardsResponse>(
                Method.GET,
                $"/basic/leaderboards/player?dbid={gamerTag}"
            );
        }

        public Promise<EmptyResponse> RemovePlayerEntry(string leaderboardId, long gamerTag)
        {
	        return ResolveAssignment(leaderboardId, gamerTag).FlatMap(assignment =>
	        {
		        using var pooledBuilder = StringBuilderPool.StaticPool.Spawn();

		        var req = new ArrayDict {{"id", gamerTag}};
		        var body = Json.Serialize(req, pooledBuilder.Builder);
		        string encodedBoardId = Requester.EscapeURL(assignment.leaderboardId);

		        return Requester.Request<EmptyResponse>(
			        Method.DELETE,
			        $"/object/leaderboards/{encodedBoardId}/entry",
			        body
		        );
	        });
        }
    }
}
