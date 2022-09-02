
namespace Beamable.Api.Open.Groups
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class GroupsApiObjectApi
    {
        private IBeamableRequester _requester;
        public GroupsApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> PutRole(string objectId, RoleChangeRequest gsReq)
        {
            string gsUrl = "/object/groups/{objectId}/role";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<GroupMembershipResponse> PostKick(string objectId, KickRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/groups/{objectId}/kick";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<GroupMembershipResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<GroupMembershipResponse>);
        }
        public virtual Promise<CommonResponse> PostApply(string objectId, GroupApplication gsReq)
        {
            string gsUrl = "/object/groups/{objectId}/apply";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<EmptyResponse> PostDonations(string objectId, CreateDonationRequest gsReq)
        {
            string gsUrl = "/object/groups/{objectId}/donations";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> PutDonations(string objectId, MakeDonationRequest gsReq)
        {
            string gsUrl = "/object/groups/{objectId}/donations";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<GroupMembershipResponse> DeleteMember(string objectId, KickRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/groups/{objectId}/member";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<GroupMembershipResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<GroupMembershipResponse>);
        }
        public virtual Promise<Group> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/groups/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<Group>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<Group>);
        }
        public virtual Promise<CommonResponse> Put(string objectId, GroupUpdate gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/groups/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> Delete(string objectId, DisbandRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/groups/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<EmptyResponse> PutDonationsClaim(string objectId)
        {
            string gsUrl = "/object/groups/{objectId}/donations/claim";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.PUT, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<CommonResponse> PostInvite(string objectId, GroupInvite gsReq)
        {
            string gsUrl = "/object/groups/{objectId}/invite";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostPetition(string objectId, GroupApplication gsReq)
        {
            string gsUrl = "/object/groups/{objectId}/petition";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
    }
}
