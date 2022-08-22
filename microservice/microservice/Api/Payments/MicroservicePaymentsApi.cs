using Beamable.Common.Api;
using Beamable.Common.Api.Payments;

namespace Beamable.Server.Api.Payments;

public class MicroservicePaymentsApi : PaymentsApi, IMicroservicePaymentsApi
{
	public MicroservicePaymentsApi(IBeamableRequester requester) : base(requester)
	{
	}
}
