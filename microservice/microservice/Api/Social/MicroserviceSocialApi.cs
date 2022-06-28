using Beamable.Common.Api;
using Beamable.Common.Api.Social;

namespace Beamable.Server.Api.Social
{
	public class MicroserviceSocialApi : SocialApi, IMicroserviceSocialApi
	{
		public MicroserviceSocialApi(IBeamableRequester requester, IUserContext ctx) : base(ctx, requester)
		{
		}
	}
}
