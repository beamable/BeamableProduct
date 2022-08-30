
namespace Beamable.Api.Open.Mail
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class MailApiBasicApi
    {
        private IBeamableRequester _requester;
        public MailApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<MailSuccessResponse> PutAttachments(AcceptMultipleAttachments gsReq)
        {
            string gsUrl = "/basic/mail/attachments";
            // make the request and return the result
            return _requester.Request<MailSuccessResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<MailSuccessResponse>);
        }
        public virtual Promise<MailTemplate> GetTemplate(string templateName, long gamerTag, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/mail/template";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("templateName=", templateName.ToString()));
            gsQueries.Add(string.Concat("gamerTag=", gamerTag.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<MailTemplate>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<MailTemplate>);
        }
        public virtual Promise<MailResponse> Get(long mid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/mail/";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("mid=", mid.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<MailResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<MailResponse>);
        }
        public virtual Promise<MailSuccessResponse> Put(UpdateMailRequest gsReq)
        {
            string gsUrl = "/basic/mail/";
            // make the request and return the result
            return _requester.Request<MailSuccessResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<MailSuccessResponse>);
        }
        public virtual Promise<MailSuccessResponse> PostBulk(BulkSendMailRequest gsReq)
        {
            string gsUrl = "/basic/mail/bulk";
            // make the request and return the result
            return _requester.Request<MailSuccessResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<MailSuccessResponse>);
        }
    }
}
