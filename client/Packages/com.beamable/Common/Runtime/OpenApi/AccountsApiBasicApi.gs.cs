
namespace Beamable.Api.Open.Accounts
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class AccountsApiBasicApi
    {
        private IBeamableRequester _requester;
        public AccountsApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<AccountPlayerView> DeleteMeDevice(DeleteDevicesRequest gsReq)
        {
            string gsUrl = "/basic/accounts/me/device";
            // make the request and return the result
            return _requester.Request<AccountPlayerView>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPlayerView>);
        }
        public virtual Promise<AccountPlayerView> GetMe()
        {
            string gsUrl = "/basic/accounts/me";
            // make the request and return the result
            return _requester.Request<AccountPlayerView>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPlayerView>);
        }
        public virtual Promise<AccountPlayerView> PutMe(AccountUpdate gsReq)
        {
            string gsUrl = "/basic/accounts/me";
            // make the request and return the result
            return _requester.Request<AccountPlayerView>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPlayerView>);
        }
        public virtual Promise<AccountPlayerView> DeleteMeThirdParty(ThirdPartyAvailableRequest gsReq)
        {
            string gsUrl = "/basic/accounts/me/third-party";
            // make the request and return the result
            return _requester.Request<AccountPlayerView>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPlayerView>);
        }
        public virtual Promise<AccountPersonallyIdentifiableInformationResponse> GetGetPersonallyIdentifiableInformation(string query, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/accounts/get-personally-identifiable-information";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("query=", query.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AccountPersonallyIdentifiableInformationResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<AccountPersonallyIdentifiableInformationResponse>);
        }
        public virtual Promise<AccountSearchResponse> GetSearch(string query, int page, int pagesize)
        {
            string gsUrl = "/basic/accounts/search";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("query=", query.ToString()));
            gsQueries.Add(string.Concat("page=", page.ToString()));
            gsQueries.Add(string.Concat("pagesize=", pagesize.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AccountSearchResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AccountSearchResponse>);
        }
        public virtual Promise<EmptyResponse> PostEmailUpdateInit(EmailUpdateRequest gsReq)
        {
            string gsUrl = "/basic/accounts/email-update/init";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> PostEmailUpdateConfirm(EmailUpdateConfirmation gsReq)
        {
            string gsUrl = "/basic/accounts/email-update/confirm";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<AccountAvailableResponse> GetAvailableThirdParty(string thirdParty, string token, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/accounts/available/third-party";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("thirdParty=", thirdParty.ToString()));
            gsQueries.Add(string.Concat("token=", token.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AccountAvailableResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<AccountAvailableResponse>);
        }
        public virtual Promise<AccountPortalView> PostAdminAdminUser(AddAccountRequest gsReq)
        {
            string gsUrl = "/basic/accounts/admin/admin-user";
            // make the request and return the result
            return _requester.Request<AccountPortalView>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPortalView>);
        }
        public virtual Promise<AccountPlayerView> PostRegister(AccountRegistration gsReq)
        {
            string gsUrl = "/basic/accounts/register";
            // make the request and return the result
            return _requester.Request<AccountPlayerView>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPlayerView>);
        }
        public virtual Promise<AccountPortalView> GetAdminMe()
        {
            string gsUrl = "/basic/accounts/admin/me";
            // make the request and return the result
            return _requester.Request<AccountPortalView>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AccountPortalView>);
        }
        public virtual Promise<EmptyResponse> PostPasswordUpdateInit(PasswordUpdateRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/accounts/password-update/init";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<GetAdminsResponse> GetAdminAdminUsers()
        {
            string gsUrl = "/basic/accounts/admin/admin-users";
            // make the request and return the result
            return _requester.Request<GetAdminsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetAdminsResponse>);
        }
        public virtual Promise<Account> GetFind(string query)
        {
            string gsUrl = "/basic/accounts/find";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("query=", query.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<Account>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<Account>);
        }
        public virtual Promise<AccountAvailableResponse> GetAvailableDeviceId(string deviceId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/accounts/available/device-id";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("deviceId=", deviceId.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AccountAvailableResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<AccountAvailableResponse>);
        }
        public virtual Promise<AccountAvailableResponse> GetAvailable(string email, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/accounts/available";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("email=", email.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AccountAvailableResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<AccountAvailableResponse>);
        }
        public virtual Promise<EmptyResponse> PostPasswordUpdateConfirm(PasswordUpdateConfirmation gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/accounts/password-update/confirm";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
    }
}
