
namespace Beamable.Api.Open.Accounts
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class AccountsApiObjectApi
    {
        private IBeamableRequester _requester;
        public AccountsApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<Account> PutAdminEmail(string objectId, EmailUpdateRequest gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/admin/email";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<Account>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<Account>);
        }
        public virtual Promise<AvailableRolesResponse> GetAvailableRoles(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/accounts/{objectId}/available-roles";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<AvailableRolesResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<AvailableRolesResponse>);
        }
        public virtual Promise<AccountRolesReport> GetRoleReport(string objectId)
        {
            string gsUrl = "/object/accounts/{objectId}/role/report";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<AccountRolesReport>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AccountRolesReport>);
        }
        public virtual Promise<EmptyResponse> PutRole(string objectId, UpdateRole gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/role";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> DeleteRole(string objectId, DeleteRole gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/role";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> PutAdminScope(string objectId, UpdateRole gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/admin/scope";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> DeleteAdminScope(string objectId, DeleteRole gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/admin/scope";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> PutAdminThirdParty(string objectId, TransferThirdPartyAssociation gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/admin/third-party";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> DeleteAdminThirdParty(string objectId, DeleteThirdPartyAssociation gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/admin/third-party";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<Account> Put(string objectId, AccountUpdate gsReq)
        {
            string gsUrl = "/object/accounts/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<Account>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<Account>);
        }
        public virtual Promise<Account> DeleteAdminForget(string objectId)
        {
            string gsUrl = "/object/accounts/{objectId}/admin/forget";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<Account>(Method.DELETE, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<Account>);
        }
    }
}
