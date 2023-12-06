using Beamable.Common;
using Beamable.Server;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Beamable.Microservices
{
	[Microservice("HathoraGameServices")]
	public class HathoraGameServices : Microservice, IFederatedGameServer<HathoraIdentity>
	{
		// TODO: Should probably move these to a configuration file in the future
		public const string appId = "app-51bbe218-6490-4c72-abac-42903444e328";
		public const string appSecret = "secret-68b338e4-0b8e-4905-957d-a1103e804c02";

		public HttpClient http = new HttpClient();

		public async Promise<ServerInfo> CreateGameServer(CreateGameServerRequest lobby)
		{
			await Task.CompletedTask;
			return new ServerInfo {globalData = new Dictionary<string, string> {{"test", "test"}}};
		}

		private Task<ConnectionInfo> CreateGameServer()
		{
			throw new NotImplementedException();
		}
	}

	public class HathoraIdentity : IThirdPartyCloudIdentity
	{
		public string UniqueName => "hathora";
	}

	[Serializable]
	public class PortInfo
	{
		public string Host { get; set; }
		public string Name { get; set; }
		public int Port { get; set; }
		public int TransportType { get; set; }
	}

	[Serializable]
	public class ConnectionInfo
	{
		public string RoomId { get; set; }
		public string Status { get; set; }
		public PortInfo ExposedPort { get; set; }
		public List<PortInfo> AdditionalExposedPorts { get; set; }
	}
}
