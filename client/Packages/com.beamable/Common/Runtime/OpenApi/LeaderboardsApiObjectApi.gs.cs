
namespace Beamable.Api.Open.Leaderboards
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface ILeaderboardsApiObjectApi
    {
        Promise<CommonResponse> DeleteEntries(string objectId);
        Promise<LeaderboardMembershipResponse> GetMembership(long playerId, string objectId);
        Promise<LeaderBoardViewResponse> GetRanks(string ids, string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<LeaderboardPartitionInfo> GetPartition(long playerId, string objectId);
        Promise<LeaderBoardViewResponse> GetFriends(string objectId);
        Promise<CommonResponse> Post(string objectId, LeaderboardCreateRequest gsReq);
        Promise<CommonResponse> Delete(string objectId);
        Promise<MatchMakingMatchesPvpResponse> GetMatches(int poolSize, int windows, int windowSize, string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<LeaderboardAssignmentInfo> GetAssignment(string objectId);
        Promise<CommonResponse> DeleteAssignment(string objectId, LeaderboardRemoveCacheEntryRequest gsReq);
        Promise<CommonResponse> PutEntry(string objectId, LeaderboardAddRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<CommonResponse> DeleteEntry(string objectId, LeaderboardRemoveEntryRequest gsReq);
        Promise<CommonResponse> PutFreeze(string objectId);
        Promise<LeaderboardDetails> GetDetails(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> from, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> max);
        Promise<LeaderBoardViewResponse> GetView(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> max, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> focus, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> friends, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> from, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> outlier, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> guild, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<CommonResponse> PutSwap(string objectId, LeaderboardSwapRequest gsReq);
    }
    public class LeaderboardsApiObjectApi : ILeaderboardsApiObjectApi
    {
        private IBeamableRequester _requester;
        public LeaderboardsApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> DeleteEntries(string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/entries";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<LeaderboardMembershipResponse> GetMembership(long playerId, string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/membership";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("playerId=", playerId.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<LeaderboardMembershipResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<LeaderboardMembershipResponse>);
        }
        public virtual Promise<LeaderBoardViewResponse> GetRanks(string ids, string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/leaderboards/{objectId}/ranks";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("ids=", ids.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<LeaderBoardViewResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<LeaderBoardViewResponse>);
        }
        public virtual Promise<LeaderboardPartitionInfo> GetPartition(long playerId, string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/partition";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("playerId=", playerId.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<LeaderboardPartitionInfo>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<LeaderboardPartitionInfo>);
        }
        public virtual Promise<LeaderBoardViewResponse> GetFriends(string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/friends";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<LeaderBoardViewResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<LeaderBoardViewResponse>);
        }
        public virtual Promise<CommonResponse> Post(string objectId, LeaderboardCreateRequest gsReq)
        {
            string gsUrl = "/object/leaderboards/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> Delete(string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<MatchMakingMatchesPvpResponse> GetMatches(int poolSize, int windows, int windowSize, string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/leaderboards/{objectId}/matches";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("poolSize=", poolSize.ToString()));
            gsQueries.Add(string.Concat("windows=", windows.ToString()));
            gsQueries.Add(string.Concat("windowSize=", windowSize.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<MatchMakingMatchesPvpResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<MatchMakingMatchesPvpResponse>);
        }
        public virtual Promise<LeaderboardAssignmentInfo> GetAssignment(string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/assignment";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<LeaderboardAssignmentInfo>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<LeaderboardAssignmentInfo>);
        }
        public virtual Promise<CommonResponse> DeleteAssignment(string objectId, LeaderboardRemoveCacheEntryRequest gsReq)
        {
            string gsUrl = "/object/leaderboards/{objectId}/assignment";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PutEntry(string objectId, LeaderboardAddRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/leaderboards/{objectId}/entry";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> DeleteEntry(string objectId, LeaderboardRemoveEntryRequest gsReq)
        {
            string gsUrl = "/object/leaderboards/{objectId}/entry";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PutFreeze(string objectId)
        {
            string gsUrl = "/object/leaderboards/{objectId}/freeze";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<LeaderboardDetails> GetDetails(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> from, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> max)
        {
            string gsUrl = "/object/leaderboards/{objectId}/details";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((from != default(OptionalInt)) 
                        && from.HasValue))
            {
                gsQueries.Add(string.Concat("from=", from.ToString()));
            }
            if (((max != default(OptionalInt)) 
                        && max.HasValue))
            {
                gsQueries.Add(string.Concat("max=", max.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<LeaderboardDetails>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<LeaderboardDetails>);
        }
        public virtual Promise<LeaderBoardViewResponse> GetView(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> max, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> focus, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> friends, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> from, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> outlier, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> guild, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/leaderboards/{objectId}/view";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((max != default(OptionalInt)) 
                        && max.HasValue))
            {
                gsQueries.Add(string.Concat("max=", max.ToString()));
            }
            if (((focus != default(OptionalLong)) 
                        && focus.HasValue))
            {
                gsQueries.Add(string.Concat("focus=", focus.ToString()));
            }
            if (((friends != default(OptionalBool)) 
                        && friends.HasValue))
            {
                gsQueries.Add(string.Concat("friends=", friends.ToString()));
            }
            if (((from != default(OptionalInt)) 
                        && from.HasValue))
            {
                gsQueries.Add(string.Concat("from=", from.ToString()));
            }
            if (((outlier != default(OptionalLong)) 
                        && outlier.HasValue))
            {
                gsQueries.Add(string.Concat("outlier=", outlier.ToString()));
            }
            if (((guild != default(OptionalBool)) 
                        && guild.HasValue))
            {
                gsQueries.Add(string.Concat("guild=", guild.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<LeaderBoardViewResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<LeaderBoardViewResponse>);
        }
        public virtual Promise<CommonResponse> PutSwap(string objectId, LeaderboardSwapRequest gsReq)
        {
            string gsUrl = "/object/leaderboards/{objectId}/swap";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
    }
}
