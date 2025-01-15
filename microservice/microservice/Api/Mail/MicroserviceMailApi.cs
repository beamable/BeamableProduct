using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Mail;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Server.Api.Mail
{
   public class MicroserviceMailApi : AbsMailApi , IMicroserviceMailApi
   {
      public BeamableGetApiResource<MailQueryResponse> _getter;

      public MicroserviceMailApi(IBeamableRequester requester, IUserContext ctx, IDependencyProvider provider) : base(requester, ctx, provider)
      {
         _getter = new BeamableGetApiResource<MailQueryResponse>();
      }

      public override Promise<MailQueryResponse> GetCurrent(string scope = "")
      {
         return _getter.RequestData(Requester, Ctx, SERVICE_NAME, scope);
      }
   }
}
