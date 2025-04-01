using Beamable.Common;
using Beamable.Player.CloudSaving;
using Beamable.Runtime.LightBeams;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[BeamContextSystem]
public class StringPage : MonoBehaviour, ILightComponent
{
	private const string SaveFileName = "string_data.sav";

	[Header("Scene References")]
	public TMP_InputField ContentStringField;
	
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

		SaveButton.HandleClicked(SaveStringData);
		LoadButton.HandleClicked(LoadStringData);
		ForceUploadButton.HandleClicked(ForceUploadData);
		ForceDownloadButton.HandleClicked(ForceDownloadData);

		UpdateServiceStatus(_cloudSavingService.ServiceStatus);

		return Promise.Success;
	}

	public void GoToHome()
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
		ContentStringField.interactable = isInitialized;
	}

	public async void SaveStringData()
	{
		await _cloudSavingService.SaveData(SaveFileName, ContentStringField.text);
	}

	private async void LoadStringData()
	{
		var result = await _cloudSavingService.LoadDataString(SaveFileName);

		if (result != null)
		{
			ContentStringField.text = result;
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
			LoadStringData();
		});
	}
}
