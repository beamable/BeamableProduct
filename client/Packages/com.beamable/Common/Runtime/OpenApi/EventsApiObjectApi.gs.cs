
namespace Beamable.Api.Open.Events
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface IEventsApiObjectApi
    {
        Promise<CommonResponse> PutEndPhase(string objectId, EventPhaseEndRequest gsReq);
        Promise<EventObjectData> Get(string objectId);
        Promise<PingRsp> GetPing(string objectId);
        Promise<CommonResponse> PutContent(string objectId, SetContentRequest gsReq);
        Promise<CommonResponse> DeleteContent(string objectId);
        Promise<CommonResponse> PutRefresh(string objectId);
    }
    public class EventsApiObjectApi : IEventsApiObjectApi
    {
        private IBeamableRequester _requester;
        public EventsApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> PutEndPhase(string objectId, EventPhaseEndRequest gsReq)
        {
            string gsUrl = "/object/events/{objectId}/endPhase";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<EventObjectData> Get(string objectId)
        {
            string gsUrl = "/object/events/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EventObjectData>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<EventObjectData>);
        }
        public virtual Promise<PingRsp> GetPing(string objectId)
        {
            string gsUrl = "/object/events/{objectId}/ping";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<PingRsp>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<PingRsp>);
        }
        public virtual Promise<CommonResponse> PutContent(string objectId, SetContentRequest gsReq)
        {
            string gsUrl = "/object/events/{objectId}/content";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> DeleteContent(string objectId)
        {
            string gsUrl = "/object/events/{objectId}/content";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PutRefresh(string objectId)
        {
            string gsUrl = "/object/events/{objectId}/refresh";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
    }
}
