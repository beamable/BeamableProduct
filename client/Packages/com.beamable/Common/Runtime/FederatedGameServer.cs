// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

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
