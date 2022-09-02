
namespace Beamable.Api.Open.Cloudsaving
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class CloudsavingApiBasicApi
    {
        private IBeamableRequester _requester;
        public CloudsavingApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<Manifest> PostDataReplace(ReplaceObjectsRequest gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/replace";
            // make the request and return the result
            return _requester.Request<Manifest>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<Manifest>);
        }
        public virtual Promise<EmptyResponse> DeleteData(ObjectRequests gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<URLSResponse> PostDataDownloadURL(ObjectRequests gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/downloadURL";
            // make the request and return the result
            return _requester.Request<URLSResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<URLSResponse>);
        }
        public virtual Promise<ObjectsMetadataResponse> GetDataMetadata([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<MapOfObject> request, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> playerId)
        {
            string gsUrl = "/basic/cloudsaving/data/metadata";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((request != default(OptionalMapOfObject)) 
                        && request.HasValue))
            {
                gsQueries.Add(string.Concat("request=", request.ToString()));
            }
            if (((playerId != default(OptionalLong)) 
                        && playerId.HasValue))
            {
                gsQueries.Add(string.Concat("playerId=", playerId.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<ObjectsMetadataResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<ObjectsMetadataResponse>);
        }
        public virtual Promise<URLSResponse> PostDataDownloadURLFromPortal(ObjectRequests gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/downloadURLFromPortal";
            // make the request and return the result
            return _requester.Request<URLSResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<URLSResponse>);
        }
        public virtual Promise<Manifest> PutDataMove(PlayerBasicCloudDataRequest gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/move";
            // make the request and return the result
            return _requester.Request<Manifest>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<Manifest>);
        }
        public virtual Promise<Manifest> PutDataMoveFromPortal(PlayerBasicCloudDataRequest gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/moveFromPortal";
            // make the request and return the result
            return _requester.Request<Manifest>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<Manifest>);
        }
        public virtual Promise<URLSResponse> PostDataUploadURLFromPortal(UploadRequestsFromPortal gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/uploadURLFromPortal";
            // make the request and return the result
            return _requester.Request<URLSResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<URLSResponse>);
        }
        public virtual Promise<Manifest> PutDataCommitManifest(UploadRequests gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/commitManifest";
            // make the request and return the result
            return _requester.Request<Manifest>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<Manifest>);
        }
        public virtual Promise<URLSResponse> PostDataUploadURL(UploadRequests gsReq)
        {
            string gsUrl = "/basic/cloudsaving/data/uploadURL";
            // make the request and return the result
            return _requester.Request<URLSResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<URLSResponse>);
        }
        public virtual Promise<Manifest> Get([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> playerId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/cloudsaving/";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((playerId != default(OptionalLong)) 
                        && playerId.HasValue))
            {
                gsQueries.Add(string.Concat("playerId=", playerId.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<Manifest>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<Manifest>);
        }
    }
}
