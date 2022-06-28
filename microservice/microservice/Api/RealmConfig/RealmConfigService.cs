using Beamable.Common;
using Beamable.Common.Api;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Beamable.Server.Api.RealmConfig
{
	public class RealmConfigService : IMicroserviceRealmConfigService
	{
		private readonly IBeamableRequester _requester;

		public RealmConfigService(IBeamableRequester requester)
		{
			_requester = requester;
		}

		private Promise<GetRealmConfigResponse> GetRealmConfig()
		{
			return _requester.Request(Method.GET, "basic/realms/config", parser: Parse);
		}

		private GetRealmConfigResponse Parse(string json)
		{
			var realmData = JsonConvert.DeserializeObject<GetRealmConfigResponse>(json);
			return realmData;
		}

		[System.Serializable]
		private class GetRealmConfigResponse
		{
			public Dictionary<string, string> config;
		}

		public Promise<RealmConfig> GetRealmConfigSettings()
		{
			return GetRealmConfig().Map(realmConfigData =>
			{
				var nameSpaceToSettings = new Dictionary<string, Dictionary<string, string>>();
				if (realmConfigData?.config == null) return RealmConfig.From(nameSpaceToSettings);

				foreach (var kvp in realmConfigData.config)
				{
					var fullKey = kvp.Key;
					var configValue = kvp.Value;
					if (string.IsNullOrEmpty(fullKey)) continue; // invalid realm key.

				 var keyParts = fullKey.Split('|');
					if (keyParts.Length != 2) continue; // invalid realm setting key.

				 var nameSpace = keyParts[0];
					var setting = keyParts[1];

					if (!nameSpaceToSettings.TryGetValue(nameSpace, out var nameSpaceSection))
					{
						nameSpaceSection = new Dictionary<string, string>();
					}

					nameSpaceSection[setting] = configValue;
					nameSpaceToSettings[nameSpace] = nameSpaceSection;
				}

				return RealmConfig.From(nameSpaceToSettings);
			});
		}
	}
}
