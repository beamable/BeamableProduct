
namespace Beamable.Api.Open.Matchmaking
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface IMatchmakingApiObjectApi
    {
        Promise<EmptyResponse> PutTick(string objectId);
        Promise<MatchUpdate> GetMatch(string objectId);
        Promise<MatchUpdate> PostMatch(string objectId);
        Promise<EmptyResponse> DeleteMatch(string objectId);
    }
    public class MatchmakingApiObjectApi : IMatchmakingApiObjectApi
    {
        private IBeamableRequester _requester;
        public MatchmakingApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<EmptyResponse> PutTick(string objectId)
        {
            string gsUrl = "/object/matchmaking/{objectId}/tick";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.PUT, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<MatchUpdate> GetMatch(string objectId)
        {
            string gsUrl = "/object/matchmaking/{objectId}/match";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<MatchUpdate>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<MatchUpdate>);
        }
        public virtual Promise<MatchUpdate> PostMatch(string objectId)
        {
            string gsUrl = "/object/matchmaking/{objectId}/match";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<MatchUpdate>(Method.POST, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<MatchUpdate>);
        }
        public virtual Promise<EmptyResponse> DeleteMatch(string objectId)
        {
            string gsUrl = "/object/matchmaking/{objectId}/match";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
    }
}
