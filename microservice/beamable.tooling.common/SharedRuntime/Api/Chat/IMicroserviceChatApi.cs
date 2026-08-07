using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Experimental.Api.Chat;
using System.Collections.Generic;

namespace Beamable.Server.Api.Chat
{

	/// <summary>
	/// The chat API for Microservice usage
	/// </summary>

	[RealmScoped]
	public interface IMicroserviceChatApi : IChatApi { }

}
