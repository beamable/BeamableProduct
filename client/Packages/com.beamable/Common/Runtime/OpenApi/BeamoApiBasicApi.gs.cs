
namespace Beamable.Api.Open.Beamo
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class BeamoApiBasicApi
    {
        private IBeamableRequester _requester;
        public BeamoApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<GetSignedUrlResponse> PostMetricsUrl(GetMetricsUrlRequest gsReq)
        {
            string gsUrl = "/basic/beamo/metricsUrl";
            // make the request and return the result
            return _requester.Request<GetSignedUrlResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<GetSignedUrlResponse>);
        }
        public virtual Promise<PerformanceResponse> GetStoragePerformance(string storageObjectName, string granularity, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> endDate, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> startDate, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> period)
        {
            string gsUrl = "/basic/beamo/storage/performance";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((endDate != default(OptionalString)) 
                        && endDate.HasValue))
            {
                gsQueries.Add(string.Concat("endDate=", endDate.ToString()));
            }
            gsQueries.Add(string.Concat("storageObjectName=", storageObjectName.ToString()));
            gsQueries.Add(string.Concat("granularity=", granularity.ToString()));
            if (((startDate != default(OptionalString)) 
                        && startDate.HasValue))
            {
                gsQueries.Add(string.Concat("startDate=", startDate.ToString()));
            }
            if (((period != default(OptionalString)) 
                        && period.HasValue))
            {
                gsQueries.Add(string.Concat("period=", period.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<PerformanceResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<PerformanceResponse>);
        }
        public virtual Promise<GetManifestsResponse> GetManifests([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> offset, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<int> limit, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> archived)
        {
            string gsUrl = "/basic/beamo/manifests";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((offset != default(OptionalInt)) 
                        && offset.HasValue))
            {
                gsQueries.Add(string.Concat("offset=", offset.ToString()));
            }
            if (((limit != default(OptionalInt)) 
                        && limit.HasValue))
            {
                gsQueries.Add(string.Concat("limit=", limit.ToString()));
            }
            if (((archived != default(OptionalBool)) 
                        && archived.HasValue))
            {
                gsQueries.Add(string.Concat("archived=", archived.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetManifestsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetManifestsResponse>);
        }
        public virtual Promise<GetTemplatesResponse> GetTemplates()
        {
            string gsUrl = "/basic/beamo/templates";
            // make the request and return the result
            return _requester.Request<GetTemplatesResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetTemplatesResponse>);
        }
        public virtual Promise<GetSignedUrlResponse> PostLogsUrl(GetLogsUrlRequest gsReq)
        {
            string gsUrl = "/basic/beamo/logsUrl";
            // make the request and return the result
            return _requester.Request<GetSignedUrlResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<GetSignedUrlResponse>);
        }
        public virtual Promise<GetLambdaURI> GetUploadAPI()
        {
            string gsUrl = "/basic/beamo/uploadAPI";
            // make the request and return the result
            return _requester.Request<GetLambdaURI>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetLambdaURI>);
        }
        public virtual Promise<GetStatusResponse> GetStatus()
        {
            string gsUrl = "/basic/beamo/status";
            // make the request and return the result
            return _requester.Request<GetStatusResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetStatusResponse>);
        }
        public virtual Promise<GetCurrentManifestResponse> GetManifestCurrent([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> archived)
        {
            string gsUrl = "/basic/beamo/manifest/current";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((archived != default(OptionalBool)) 
                        && archived.HasValue))
            {
                gsQueries.Add(string.Concat("archived=", archived.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetCurrentManifestResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetCurrentManifestResponse>);
        }
        public virtual Promise<ManifestChecksums> PostManifestPull(PullBeamoManifestRequest gsReq)
        {
            string gsUrl = "/basic/beamo/manifest/pull";
            // make the request and return the result
            return _requester.Request<ManifestChecksums>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<ManifestChecksums>);
        }
        public virtual Promise<GetElasticContainerRegistryURI> GetRegistry()
        {
            string gsUrl = "/basic/beamo/registry";
            // make the request and return the result
            return _requester.Request<GetElasticContainerRegistryURI>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetElasticContainerRegistryURI>);
        }
        public virtual Promise<EmptyResponse> PostManifestDeploy()
        {
            string gsUrl = "/basic/beamo/manifest/deploy";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<ConnectionString> GetStorageConnection()
        {
            string gsUrl = "/basic/beamo/storage/connection";
            // make the request and return the result
            return _requester.Request<ConnectionString>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<ConnectionString>);
        }
        public virtual Promise<GetManifestResponse> GetManifest(string id, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> archived)
        {
            string gsUrl = "/basic/beamo/manifest";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("id=", id.ToString()));
            if (((archived != default(OptionalBool)) 
                        && archived.HasValue))
            {
                gsQueries.Add(string.Concat("archived=", archived.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetManifestResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetManifestResponse>);
        }
        public virtual Promise<PostManifestResponse> PostManifest(PostManifestRequest gsReq)
        {
            string gsUrl = "/basic/beamo/manifest";
            // make the request and return the result
            return _requester.Request<PostManifestResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<PostManifestResponse>);
        }
    }
}
