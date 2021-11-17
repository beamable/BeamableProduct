using Beamable.Common;
using Beamable.Common.Api.CloudData;
using Beamable.ConsoleCommands;
using Beamable.Service;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace Beamable.Api.CloudData
{
	[BeamableConsoleCommandProvider]
	public class CloudDataConsoleCommands
	{
		private BeamableConsole Console => ServiceManager.Resolve<BeamableConsole>();

		[Preserve]
		public CloudDataConsoleCommands()
		{
		}

		[BeamableConsoleCommand("CLOUD-MANIFEST", "Fetch the game cloud manifest", "CLOUD-MANIFEST")]
		protected string GetManifest(params string[] args)
		{
			var platform = ServiceManager.Resolve<PlatformService>();
			platform.CloudDataService.GetGameManifest().Then(response =>
			{
				string json = JsonUtility.ToJson(response);
				Debug.Log($"Game Cloud Manifest: {json}");
			});

			return "Fetching Cloud Game Manifest...";
		}

		[BeamableConsoleCommand("CLOUD-PLAYER", "Fetch the player cloud manifest", "CLOUD-PLAYER")]
		protected string GetPlayerManifest(params string[] args)
		{
			var platform = ServiceManager.Resolve<PlatformService>();
			platform.CloudDataService.GetPlayerManifest().Then(response =>
			{
				string json = JsonUtility.ToJson(response);
				Debug.Log($"Player Cloud Manifest for current player: {json}");
			});

			return $"Fetching Cloud Player Manifest for current player...";
		}
	}

	[System.Serializable]
	public class TestCloudData
	{
		public string name;
	}
}
