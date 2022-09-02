
namespace Beamable.Api.Open.Auth
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class AuthApiBasicApi
    {
        private IBeamableRequester _requester;
        public AuthApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<ListTokenResponse> GetTokenList(int pageSize, int page, long gamerTagOrAccountId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> cid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> pid)
        {
            string gsUrl = "/basic/auth/token/list";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("pageSize=", pageSize.ToString()));
            gsQueries.Add(string.Concat("page=", page.ToString()));
            if (((cid != default(OptionalLong)) 
                        && cid.HasValue))
            {
                gsQueries.Add(string.Concat("cid=", cid.ToString()));
            }
            if (((pid != default(OptionalString)) 
                        && pid.HasValue))
            {
                gsQueries.Add(string.Concat("pid=", pid.ToString()));
            }
            gsQueries.Add(string.Concat("gamerTagOrAccountId=", gamerTagOrAccountId.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<ListTokenResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<ListTokenResponse>);
        }
        public virtual Promise<Token> GetToken(string token, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/auth/token";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("token=", token.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<Token>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<Token>);
        }
        public virtual Promise<TokenResponse> PostToken(TokenRequestWrapper gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/auth/token";
            // make the request and return the result
            return _requester.Request<TokenResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<TokenResponse>);
        }
        public virtual Promise<CommonResponse> PutTokenRevoke(RevokeTokenRequest gsReq)
        {
            string gsUrl = "/basic/auth/token/revoke";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
    }
}
