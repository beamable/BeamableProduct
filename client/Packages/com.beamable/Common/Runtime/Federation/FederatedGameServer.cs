// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

using Beamable.Common.BeamCli;
using System;
using System.Collections.Generic;
using Lobby = Beamable.Experimental.Api.Lobbies.Lobby;

namespace Beamable.Common
{
	public interface IFederatedGameServer<in T> : IFederation where T : IFederationId, new()
	{
		Promise<ServerInfo> CreateGameServer(Lobby lobby);
	}

	[Serializable, CliContractType]
	public class LocalSettings_IFederatedGameServer : IFederation.ILocalSettings
	{
		public string[] contentIds;
	}

	[Serializable]
	public class ServerInfo
	{
		public Dictionary<string, string> globalData;
		public Dictionary<string, Dictionary<string, string>> playerData;
	}
}
