using Beamable.Common.Api;
using Beamable.Common.Api.CloudData;

namespace Beamable.Server.Api.CloudData
{
	public class MicroserviceCloudDataApi : CloudDataApi, IMicroserviceCloudDataApi
	{
		public MicroserviceCloudDataApi(IBeamableRequester requester, IUserContext ctx) : base(ctx, requester)
		{
		}
	}
}
