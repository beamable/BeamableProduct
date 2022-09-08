using Beamable.Api.Analytics;
using Beamable.Common.Api;

namespace Beamable.Server.Api.Analytics
{
	public class MicroserviceAnalyticsService : AnalyticsService, IMicroserviceAnalyticsService
	{
		public MicroserviceAnalyticsService(IUserContext context, IBeamableRequester requester) : base(context, requester)
		{}
	}
}
