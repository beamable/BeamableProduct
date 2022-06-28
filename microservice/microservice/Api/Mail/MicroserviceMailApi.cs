using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Mail;

namespace Beamable.Server.Api.Mail
{
	public class MicroserviceMailApi : AbsMailApi, IMicroserviceMailApi
	{
		public BeamableGetApiResource<MailQueryResponse> _getter;

		public MicroserviceMailApi(IBeamableRequester requester, IUserContext ctx) : base(requester, ctx)
		{
			_getter = new BeamableGetApiResource<MailQueryResponse>();
		}

		public override Promise<MailQueryResponse> GetCurrent(string scope = "")
		{
			return _getter.RequestData(Requester, Ctx, SERVICE_NAME, scope);
		}
	}
}
