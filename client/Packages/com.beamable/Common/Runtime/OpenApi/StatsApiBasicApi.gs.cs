
namespace Beamable.Api.Open.Stats
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface IStatsApiBasicApi
    {
        Promise<CommonResponse> PutSubscribe(StatsSubscribeRequest gsReq);
        Promise<BatchReadStatsResponse> GetClientBatch(string objectIds, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stats, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> format, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<EmptyResponse> PostBatch(BatchSetStatsRequest gsReq);
        Promise<StatsSearchResponse> PostSearch(StatsSearchRequest gsReq);
        Promise<SearchExtendedResponse> PostSearchExtended(SearchExtendedRequest gsReq);
    }
    public class StatsApiBasicApi : IStatsApiBasicApi
    {
        private IBeamableRequester _requester;
        public StatsApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> PutSubscribe(StatsSubscribeRequest gsReq)
        {
            string gsUrl = "/basic/stats/subscribe";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<BatchReadStatsResponse> GetClientBatch(string objectIds, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stats, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> format, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/stats/client/batch";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("objectIds=", objectIds.ToString()));
            if (((stats != default(OptionalString)) 
                        && stats.HasValue))
            {
                gsQueries.Add(string.Concat("stats=", stats.ToString()));
            }
            if (((format != default(OptionalString)) 
                        && format.HasValue))
            {
                gsQueries.Add(string.Concat("format=", format.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<BatchReadStatsResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<BatchReadStatsResponse>);
        }
        public virtual Promise<EmptyResponse> PostBatch(BatchSetStatsRequest gsReq)
        {
            string gsUrl = "/basic/stats/batch";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<StatsSearchResponse> PostSearch(StatsSearchRequest gsReq)
        {
            string gsUrl = "/basic/stats/search";
            // make the request and return the result
            return _requester.Request<StatsSearchResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<StatsSearchResponse>);
        }
        public virtual Promise<SearchExtendedResponse> PostSearchExtended(SearchExtendedRequest gsReq)
        {
            string gsUrl = "/basic/stats/search/extended";
            // make the request and return the result
            return _requester.Request<SearchExtendedResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<SearchExtendedResponse>);
        }
    }
}
