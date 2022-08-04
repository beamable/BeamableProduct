using Beamable.Common.Api;
using Beamable.Experimental.Api.Chat;

namespace Beamable.Server.Api.Chat;

public class MicroserviceChatApi : ChatApi, IMicroserviceChatApi
{
	public MicroserviceChatApi(IBeamableRequester requester, IUserContext userContext) : base(requester, userContext)
	{
	}
}
