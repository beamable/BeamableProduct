
namespace Beamable.Api.Open.Announcements
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface IAnnouncementsApiObjectApi
    {
        Promise<CommonResponse> PutRead(string objectId, AnnouncementRequest gsReq);
        Promise<CommonResponse> PostClaim(string objectId, AnnouncementRequest gsReq);
        Promise<AnnouncementRawResponse> GetRaw(string objectId);
        Promise<AnnouncementQueryResponse> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> include_deleted);
        Promise<CommonResponse> Delete(string objectId, AnnouncementRequest gsReq);
    }
    public class AnnouncementsApiObjectApi : IAnnouncementsApiObjectApi
    {
        private IBeamableRequester _requester;
        public AnnouncementsApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> PutRead(string objectId, AnnouncementRequest gsReq)
        {
            string gsUrl = "/object/announcements/{objectId}/read";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostClaim(string objectId, AnnouncementRequest gsReq)
        {
            string gsUrl = "/object/announcements/{objectId}/claim";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<AnnouncementRawResponse> GetRaw(string objectId)
        {
            string gsUrl = "/object/announcements/{objectId}/raw";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<AnnouncementRawResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AnnouncementRawResponse>);
        }
        public virtual Promise<AnnouncementQueryResponse> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<bool> include_deleted)
        {
            string gsUrl = "/object/announcements/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((include_deleted != default(OptionalBool)) 
                        && include_deleted.HasValue))
            {
                gsQueries.Add(string.Concat("include_deleted=", include_deleted.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AnnouncementQueryResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AnnouncementQueryResponse>);
        }
        public virtual Promise<CommonResponse> Delete(string objectId, AnnouncementRequest gsReq)
        {
            string gsUrl = "/object/announcements/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
    }
}
