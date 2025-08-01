// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 

using Beamable.Common.BeamCli;
using System;
using System.Collections.Generic;
using Beamable.Api.Autogenerated.Models;

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
