using Beamable;
using Beamable.Api.CloudSaving;
using Beamable.Common;
using System;
using UnityEngine;

namespace GameExample
{
	[Serializable]
	public struct PlayerData
	{
		public int gamePoints;
		public int gamesStarted;
		public bool isThatThingEnabled;
	}

	public class PlayerManager : MonoBehaviour
	{
		public BeamContext Context;
		public PlayerData Data;

		public PlayerSaves _cloudSave;
		public CloudSavingService _CloudSavingService;
		public bool useOldUploadService;

		async void Start()
		{
			Context = BeamContext.Default;
			await Context.OnReady;
			_cloudSave = Context.ServiceProvider.GetService<PlayerSaves>();
			_CloudSavingService = Context.ServiceProvider.GetService<CloudSavingService>();
			await _CloudSavingService.Init();
			_cloudSave.OnUpdated += HandleUpdateReceived;
			await _cloudSave.Refresh();
			Data = _cloudSave.Get<PlayerData>(nameof(PlayerData));
			Debug.Log($"Init completed: {Data.ToString()}");
			Data.gamesStarted++;
			await Send();
		}

		private void HandleUpdateReceived()
		{
			Debug.Log("Update received!");
		}

		[ContextMenu(nameof(Send))]
		public async Promise Send()
		{
			if (useOldUploadService)
				await _CloudSavingService.Set(nameof(PlayerData), Data, autoUpload: true);
			else
				await _cloudSave.Set(nameof(PlayerData), Data, autoUpload: true);
		}
	}
}
