
namespace Beamable.Api.Open.Chatv2
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class Chatv2ApiObjectApi
    {
        private IBeamableRequester _requester;
        public Chatv2ApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<GetRoomsResponse> GetRooms(string objectId)
        {
            string gsUrl = "/object/chatV2/{objectId}/rooms";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<GetRoomsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetRoomsResponse>);
        }
        public virtual Promise<CreateRoomResponse> PostRooms(string objectId, CreateRoomRequest gsReq)
        {
            string gsUrl = "/object/chatV2/{objectId}/rooms";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CreateRoomResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CreateRoomResponse>);
        }
        public virtual Promise<LeaveRoomResponse> DeleteRooms(string objectId, LeaveRoomRequest gsReq)
        {
            string gsUrl = "/object/chatV2/{objectId}/rooms";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<LeaveRoomResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<LeaveRoomResponse>);
        }
        public virtual Promise<GetRoomsResponse> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> scope)
        {
            string gsUrl = "/object/chatV2/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((scope != default(OptionalString)) 
                        && scope.HasValue))
            {
                gsQueries.Add(string.Concat("scope=", scope.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetRoomsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetRoomsResponse>);
        }
        public virtual Promise<SendMessageResponse> PostMessages(string objectId, SendMessageRequest gsReq)
        {
            string gsUrl = "/object/chatV2/{objectId}/messages";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<SendMessageResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<SendMessageResponse>);
        }
    }
}
