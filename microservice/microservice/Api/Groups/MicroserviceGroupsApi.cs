using Beamable.Common.Api;
using Beamable.Common.Api.Groups;

namespace Beamable.Server.Api.Groups
{
	public class MicroserviceGroupsApi : GroupsApi, IMicroserviceGroupsApi
	{
		public MicroserviceGroupsApi(IBeamableRequester requester, IUserContext ctx) : base(ctx, requester)
		{
		}
	}
}
