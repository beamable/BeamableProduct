
namespace Beamable.Api.Open.Announcements
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface IAnnouncementsApiBasicApi
    {
        Promise<AnnouncementContentResponse> GetList();
        Promise<AnnouncementContentResponse> GetSearch([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> date);
        Promise<ListDefinitionsResponse> GetListDefinitions();
        Promise<EmptyResponse> Post(AnnouncementDto gsReq);
        Promise<EmptyResponse> Delete(DeleteAnnouncementRequest gsReq);
        Promise<AnnouncementContentResponse> GetContent();
    }
    public class AnnouncementsApiBasicApi : IAnnouncementsApiBasicApi
    {
        private IBeamableRequester _requester;
        public AnnouncementsApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<AnnouncementContentResponse> GetList()
        {
            string gsUrl = "/basic/announcements/list";
            // make the request and return the result
            return _requester.Request<AnnouncementContentResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AnnouncementContentResponse>);
        }
        public virtual Promise<AnnouncementContentResponse> GetSearch([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> date)
        {
            string gsUrl = "/basic/announcements/search";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((date != default(OptionalString)) 
                        && date.HasValue))
            {
                gsQueries.Add(string.Concat("date=", date.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AnnouncementContentResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AnnouncementContentResponse>);
        }
        public virtual Promise<ListDefinitionsResponse> GetListDefinitions()
        {
            string gsUrl = "/basic/announcements/list/definitions";
            // make the request and return the result
            return _requester.Request<ListDefinitionsResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<ListDefinitionsResponse>);
        }
        public virtual Promise<EmptyResponse> Post(AnnouncementDto gsReq)
        {
            string gsUrl = "/basic/announcements/";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> Delete(DeleteAnnouncementRequest gsReq)
        {
            string gsUrl = "/basic/announcements/";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<AnnouncementContentResponse> GetContent()
        {
            string gsUrl = "/basic/announcements/content";
            // make the request and return the result
            return _requester.Request<AnnouncementContentResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<AnnouncementContentResponse>);
        }
    }
}
