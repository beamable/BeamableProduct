using Beamable.Common;
using Beamable.Server;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Microservices
{
	[Microservice("HathoraGameServices")]
	public class HathoraGameServices : Microservice, IFederatedGameServer<HathoraIdentity>
	{
		// TODO: Should probably move these to a configuration file in the future
		private const string appId = "app-51bbe218-6490-4c72-abac-42903444e328";
		private const string appSecret = "secret-68b338e4-0b8e-4905-957d-a1103e804c02";
		// TODO: This likely needs to be configurable. Not sure where the best place to put that is.
		private const string defaultRegion = "Seattle";
		private static Uri hathoraBaseUri = new Uri("https://api.hathora.dev");

		private HttpClient http = new HttpClient();

		public async Promise<ServerInfo> CreateGameServer(CreateGameServerRequest lobby)
		{
			string roomId = lobby.lobby.lobbyId;
			ConnectionInfo response = await CreateGameServer(roomId, defaultRegion);

			while (response.status != "active")
			{
				response = await GetConnectionInfo(roomId);
				await Task.Delay(TimeSpan.FromSeconds(.5));
			}

			return new ServerInfo
			{
				globalData = new Dictionary<string, string>
				{
					{"roomId", response.roomId},
					{"host", response.exposedPort.host},
					{"name", response.exposedPort.name},
					{"port", response.exposedPort.port.ToString() },
					{"transportType", response.exposedPort.transportType}
				}
			};
		}

		private async Task<ConnectionInfo> CreateGameServer(string roomId, string region)
		{
			var createUri = new Uri(hathoraBaseUri, $"/rooms/v2/{appId}/create?roomId={roomId}") ;
			var body = new CreateRoomRequest
			{
				region = region
			};
			var content = new StringContent(JsonUtility.ToJson(body));
			HttpResponseMessage response = await http.PostAsync(createUri, content);
			return JsonUtility.FromJson<ConnectionInfo>(await response.Content.ReadAsStringAsync());
		}

		private async Task<ConnectionInfo> GetConnectionInfo(string roomId)
		{
			var getUri = new Uri(hathoraBaseUri, $"/rooms/v2/{appId}/connectioninfo/{roomId}");
			HttpResponseMessage response = await http.GetAsync(getUri);
			return JsonUtility.FromJson<ConnectionInfo>(await response.Content.ReadAsStringAsync());
		}
	}

	public class HathoraIdentity : IThirdPartyCloudIdentity
	{
		public string UniqueName => "hathora";
	}

	[Serializable]
	public class CreateRoomRequest
	{
		public string roomConfig;
		public string region;
	}

	[Serializable]
	public class PortInfo
	{
		public string host;
		public string name;
		public int port;
		public string transportType;
	}

	[Serializable]
	public class ConnectionInfo
	{
		public string roomId;
		public string status;
		public PortInfo exposedPort;
		public List<PortInfo> additionalExposedPorts;
	}
}
