// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 

using Beamable.Experimental.Api.Lobbies;
using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	public interface IFederatedGameServer<in T> where T : IThirdPartyCloudIdentity, new()
	{
		Promise<ServerInfo> CreateGameServer(Lobby lobby);
	}

	[Serializable]
	public class ServerInfo
	{
		public Dictionary<string, string> globalData;
		public Dictionary<string, Dictionary<string, string>> playerData;
	}
}
