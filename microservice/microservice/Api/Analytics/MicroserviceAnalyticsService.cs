using Beamable.Api.Analytics;
using Beamable.Common.Api;

namespace Beamable.Server.Api.Analytics
{
	public class MicroserviceAnalyticsService : AnalyticsService, IMicroserviceAnalyticsService
	{
		public MicroserviceAnalyticsService(IUserContext context, ISignedRequester requester) : base(context, requester)
		{}
	}
}
