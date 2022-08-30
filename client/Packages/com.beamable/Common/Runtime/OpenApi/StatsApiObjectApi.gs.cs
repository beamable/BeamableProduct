
namespace Beamable.Api.Open.Stats
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class StatsApiObjectApi
    {
        private IBeamableRequester _requester;
        public StatsApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<EmptyResponse> PostClientStringlist(string objectId, StatUpdateRequestStringListFormat gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/stats/{objectId}/client/stringlist";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<StatsResponse> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stats)
        {
            string gsUrl = "/object/stats/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((stats != default(OptionalString)) 
                        && stats.HasValue))
            {
                gsQueries.Add(string.Concat("stats=", stats.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<StatsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<StatsResponse>);
        }
        public virtual Promise<EmptyResponse> Post(string objectId, StatUpdateRequest gsReq)
        {
            string gsUrl = "/object/stats/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> Delete(string objectId, StatRequest gsReq)
        {
            string gsUrl = "/object/stats/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<StatsResponse> GetClient(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stats, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/stats/{objectId}/client";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((stats != default(OptionalString)) 
                        && stats.HasValue))
            {
                gsQueries.Add(string.Concat("stats=", stats.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<StatsResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<StatsResponse>);
        }
        public virtual Promise<EmptyResponse> PostClient(string objectId, StatUpdateRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/stats/{objectId}/client";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
    }
}
