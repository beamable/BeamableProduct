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

		CloudSavingService _cloudSave;

		async void Start()
		{
			Context = BeamContext.Default;
			await Context.OnReady;
			_cloudSave = Context.ServiceProvider.GetService<CloudSavingService>();
			_cloudSave.UpdateReceived += HandleUpdateReceived;
			Data = await _cloudSave.Get<PlayerData>(nameof(PlayerData));
			Debug.Log($"Init completed: {Data.ToString()}");
			Data.gamesStarted++;
			await Send();
		}

		private void HandleUpdateReceived(ManifestResponse obj)
		{
			throw new NotImplementedException();
		}

		[ContextMenu("DUA")]
		public async Promise Send()
		{
			await _cloudSave.Set(nameof(PlayerData), Data, autoUpload: true);
		}
	}
}
