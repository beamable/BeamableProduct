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

            var ttlValue = (long?)null;
            if (ttl != null && ttl.HasValue)
                ttlValue = ttl.Value;

            var derivativesValue = default(List<string>);
            if (derivatives != null && derivatives.HasValue)
                derivativesValue = derivatives.Value;

            var freezeValue = (long?) null;
            if (freezeTime != null && freezeTime.HasValue)
                freezeValue = freezeTime.Value;

            var request = new CreateLeaderboardRequest()
            {
                maxEntries = maxEntries.Value,
                ttl = ttlValue,
                partitioned = partitioned.Value,
                cohortSettings = cohorts.Value,
                derivatives = derivativesValue,
                permissions = permissions,
                freezeTime = freezeValue,
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
                maxEntries = (maxEntries ?? new OptionalInt()).Value,
                ttl = (ttl ?? new OptionalLong()).Value,
                partitioned = (partitioned ?? new OptionalBoolean()).Value,
                cohortSettings = (cohortSettings ?? new OptionalCohortSettings()).Value,
                derivatives = (derivatives ?? new OptionalListString()).Value,
                permissions = (permissions ?? new OptionalClientPermissions()).Value,
                freezeTime = (freezeTime ?? new OptionalLong()).Value
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

                    arrayDict.Add("statRequirements", cohort.statRequirements.Select(delegate(StatRequirement requirement)
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

            if(req.freezeTime.HasValue) dict.Add("freezeTime", req.freezeTime.Value);

            var json = Json.Serialize(dict, pooledBuilder.Builder);
            return Requester.Request<EmptyResponse>(Method.POST, $"{SERVICE_PATH}/{leaderboardId}", json);
        }
    }
}