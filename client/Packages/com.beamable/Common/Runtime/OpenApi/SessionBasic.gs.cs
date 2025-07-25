// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0


namespace Beamable.Api.Autogenerated.Session
{
    using Beamable.Api.Autogenerated.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    using Beamable.Common.Dependencies;
    
    public partial interface ISessionApi
    {
        /// <summary>
        /// POST call to `/basic/session/heartbeat` endpoint.
        /// </summary>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="SessionHeartbeat"/></returns>
        Promise<SessionHeartbeat> PostHeartbeat([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// GET call to `/basic/session/history` endpoint.
        /// </summary>
        /// <param name="dbid"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="SessionHistoryResponse"/></returns>
        Promise<SessionHistoryResponse> GetHistory(long dbid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> month, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> year, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// GET call to `/basic/session/status` endpoint.
        /// </summary>
        /// <param name="intervalSecs"></param>
        /// <param name="playerIds"></param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="OnlineStatusResponses"/></returns>
        Promise<OnlineStatusResponses> GetStatus(long intervalSecs, string playerIds, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// GET call to `/basic/session/client/history` endpoint.
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="SessionClientHistoryResponse"/></returns>
        Promise<SessionClientHistoryResponse> GetClientHistory([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> month, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> year, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// POST call to `/basic/session/` endpoint.
        /// </summary>
        /// <param name="gsReq">The <see cref="StartSessionRequest"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="StartSessionResponse"/></returns>
        Promise<StartSessionResponse> Post(StartSessionRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
    }
    public partial class SessionApi : ISessionApi
    {
        /// <summary>
        /// POST call to `/basic/session/heartbeat` endpoint.
        /// </summary>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="SessionHeartbeat"/></returns>
        public virtual Promise<SessionHeartbeat> PostHeartbeat([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/session/heartbeat";
            // make the request and return the result
            return _requester.Request<SessionHeartbeat>(Method.POST, gsUrl, default(object), includeAuthHeader, this.Serialize<SessionHeartbeat>);
        }
        /// <summary>
        /// GET call to `/basic/session/history` endpoint.
        /// </summary>
        /// <param name="dbid"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="SessionHistoryResponse"/></returns>
        public virtual Promise<SessionHistoryResponse> GetHistory(long dbid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> month, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> year, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/session/history";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("dbid=", _requester.EscapeURL(dbid.ToString())));
            if (((month != default(OptionalInt)) 
                        && month.HasValue))
            {
                gsQueries.Add(string.Concat("month=", month.Value.ToString()));
            }
            if (((year != default(OptionalInt)) 
                        && year.HasValue))
            {
                gsQueries.Add(string.Concat("year=", year.Value.ToString()));
            }
            if ((gsQueries.Count > 0))
            {
                gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
                gsUrl = string.Concat(gsUrl, gsQuery);
            }
            // make the request and return the result
            return _requester.Request<SessionHistoryResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, this.Serialize<SessionHistoryResponse>);
        }
        /// <summary>
        /// GET call to `/basic/session/status` endpoint.
        /// </summary>
        /// <param name="intervalSecs"></param>
        /// <param name="playerIds"></param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="OnlineStatusResponses"/></returns>
        public virtual Promise<OnlineStatusResponses> GetStatus(long intervalSecs, string playerIds, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/session/status";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("playerIds=", _requester.EscapeURL(playerIds.ToString())));
            gsQueries.Add(string.Concat("intervalSecs=", _requester.EscapeURL(intervalSecs.ToString())));
            if ((gsQueries.Count > 0))
            {
                gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
                gsUrl = string.Concat(gsUrl, gsQuery);
            }
            // make the request and return the result
            return _requester.Request<OnlineStatusResponses>(Method.GET, gsUrl, default(object), includeAuthHeader, this.Serialize<OnlineStatusResponses>);
        }
        /// <summary>
        /// GET call to `/basic/session/client/history` endpoint.
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="SessionClientHistoryResponse"/></returns>
        public virtual Promise<SessionClientHistoryResponse> GetClientHistory([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> month, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> year, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/session/client/history";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((month != default(OptionalInt)) 
                        && month.HasValue))
            {
                gsQueries.Add(string.Concat("month=", month.Value.ToString()));
            }
            if (((year != default(OptionalInt)) 
                        && year.HasValue))
            {
                gsQueries.Add(string.Concat("year=", year.Value.ToString()));
            }
            if ((gsQueries.Count > 0))
            {
                gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
                gsUrl = string.Concat(gsUrl, gsQuery);
            }
            // make the request and return the result
            return _requester.Request<SessionClientHistoryResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, this.Serialize<SessionClientHistoryResponse>);
        }
        /// <summary>
        /// POST call to `/basic/session/` endpoint.
        /// </summary>
        /// <param name="gsReq">The <see cref="StartSessionRequest"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="StartSessionResponse"/></returns>
        public virtual Promise<StartSessionResponse> Post(StartSessionRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/session/";
            // make the request and return the result
            return _requester.Request<StartSessionResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<StartSessionResponse>);
        }
    }
}
