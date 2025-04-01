using Beamable.Common;
using Beamable.Player.CloudSaving;
using Beamable.Runtime.LightBeams;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[BeamContextSystem]
public class JsonPage : MonoBehaviour, ILightComponent
{
	private const string SaveFileName = "campaignData.sav";

	[Header("Scene References")]
	public TMP_InputField LevelInput;

	public TMP_Dropdown DifficultyDropdown;
	public TMP_Dropdown ClassDropdown;
	public Button BackButton;
	public Button SaveButton;
	public Button LoadButton;
	public Button ForceUploadButton;
	public Button ForceDownloadButton;

	public TextMeshProUGUI _serviceStatus;
	public TextMeshProUGUI playerId;

	private LightBeam _ctx;
	private ICloudSavingService _cloudSavingService;


	public Promise OnInstantiated(LightBeam ctx)
	{
		_ctx = ctx;
		playerId.text = $"Player Id: {ctx.BeamContext.PlayerId}";
		BackButton.HandleClicked(GoToHome);
		_cloudSavingService = _ctx.Scope.GetService<ICloudSavingService>();

		DifficultyDropdown.AddOptions(Enum.GetNames(typeof(Difficulty)).ToList());
		ClassDropdown.AddOptions(Enum.GetNames(typeof(CharacterClass)).ToList());
		
		SaveButton.HandleClicked(SaveCampaignData);
		LoadButton.HandleClicked(LoadCampaignData);
		ForceUploadButton.HandleClicked(ForceUploadData);
		ForceDownloadButton.HandleClicked(ForceDownloadData);

		UpdateServiceStatus(_cloudSavingService.ServiceStatus);

		return Promise.Success;
	}

	private void GoToHome()
	{
		_ctx.GotoPage<HomePage>();
	}

	private void UpdateServiceStatus(CloudSaveStatus serviceStatus)
	{
		_serviceStatus.text = $"Service Status: {serviceStatus}";
		bool isInitialized = serviceStatus == CloudSaveStatus.Initialized;
		SaveButton.interactable = isInitialized;
		LoadButton.interactable = isInitialized;
		ForceUploadButton.interactable = isInitialized;
		ForceDownloadButton.interactable = isInitialized;
		LevelInput.interactable = isInitialized;
		DifficultyDropdown.interactable = isInitialized;
		ClassDropdown.interactable = isInitialized;
	}

	public async void SaveCampaignData()
	{
		var data = new CampaignData
		{
			Level = int.TryParse(LevelInput.text, out int level) ? level : 1,
			Difficulty = (Difficulty)DifficultyDropdown.value,
			MainClass = (CharacterClass)ClassDropdown.value
		};

		await _cloudSavingService.SaveData(SaveFileName, data);
	}

	private async void LoadCampaignData()
	{
		var result = await _cloudSavingService.LoadData<CampaignData>(SaveFileName);

		if (result != null)
		{
			LevelInput.text = result.Level.ToString();
			DifficultyDropdown.value =
				DifficultyDropdown.options.FindIndex(o => o.text == result.Difficulty.ToString());
			ClassDropdown.value = ClassDropdown.options.FindIndex(o => o.text == result.MainClass.ToString());
		}
	}

	private void ForceUploadData()
	{
		_cloudSavingService.ForceUploadLocalData();
	}

	private void ForceDownloadData()
	{
		_cloudSavingService.ForceDownloadCloudData().Then(_ =>
		{
			LoadCampaignData();
		});
	}


	[Serializable]
	private enum Difficulty
	{
		Easy,
		Medium,
		Hard,
	}

	[Serializable]
	private enum CharacterClass
	{
		Fighter,
		Druid,
		Cleric,
		Sorcerer,
		Barbarian,
		Bard,
		Ranger,
		Monk,
		Rogue,
		Paladin,
		Wizard,
		Warlock,
	}

	[Serializable]
	private class CampaignData
	{
		public int Level;
		public Difficulty Difficulty;
		public CharacterClass MainClass;
	}
}
