using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Commerce;
using System.Collections.Generic;


namespace Beamable.Server.Api.Commerce
{
	public class MicroserviceCommerceApi : IMicroserviceCommerceApi
	{
		private readonly IBeamableRequester _requester;

		public MicroserviceCommerceApi(IBeamableRequester requester)
		{
			_requester = requester;
		}

		public Promise<Unit> AccelerateListingCooldown(long gamerTag, List<CooldownReductionRequest> cooldownReductions)
		{
			var request = new UpdateListingCooldownRequest(gamerTag, cooldownReductions);

			return _requester.Request<Unit>(
			   Method.PUT,
			   $"/object/commerce/{gamerTag}/listings/cooldown",
			   request
			);

		}
	}
}
