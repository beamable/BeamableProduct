
namespace Beamable.Api.Open.Notification
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class NotificationApiBasicApi
    {
        private IBeamableRequester _requester;
        public NotificationApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> PostPlayer(NotificationRequest gsReq)
        {
            string gsUrl = "/basic/notification/player";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostCustom(NotificationRequest gsReq)
        {
            string gsUrl = "/basic/notification/custom";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostServer(ServerEvent gsReq)
        {
            string gsUrl = "/basic/notification/server";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostGeneric(NotificationRequest gsReq)
        {
            string gsUrl = "/basic/notification/generic";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<SubscriberDetailsResponse> Get()
        {
            string gsUrl = "/basic/notification/";
            // make the request and return the result
            return _requester.Request<SubscriberDetailsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<SubscriberDetailsResponse>);
        }
        public virtual Promise<CommonResponse> PostGame(NotificationRequest gsReq)
        {
            string gsUrl = "/basic/notification/game";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
    }
}
