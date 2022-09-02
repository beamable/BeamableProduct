
namespace Beamable.Api.Open.Push
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class PushApiBasicApi
    {
        private IBeamableRequester _requester;
        public PushApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<EmptyRsp> PostRegister(RegisterReq gsReq)
        {
            string gsUrl = "/basic/push/register";
            // make the request and return the result
            return _requester.Request<EmptyRsp>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyRsp>);
        }
        public virtual Promise<EmptyRsp> PostSend(SendReq gsReq)
        {
            string gsUrl = "/basic/push/send";
            // make the request and return the result
            return _requester.Request<EmptyRsp>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyRsp>);
        }
    }
}
