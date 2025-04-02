using Beamable.Common;
using Beamable.Player.CloudSaving;
using Beamable.Runtime.LightBeams;
using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[BeamContextSystem]
public class BytePage : MonoBehaviour, ILightComponent
{
	private const string SaveFileName = "byte_data.sav";

	[Header("Scene References")]
	public TMP_InputField ContentData;
	public TextMeshProUGUI ByteDataText;
	
	public Button BackButton;
	public Button SaveButton;
	public Button LoadButton;
	public Button ForceUploadButton;
	public Button ForceDownloadButton;

	public TextMeshProUGUI _serviceStatus;
	public TextMeshProUGUI playerId;

	private LightBeam _ctx;
	private ICloudSavingService _cloudSavingService;
	private byte[] _byteValue;

	public Promise OnInstantiated(LightBeam ctx)
	{
		_ctx = ctx;
		playerId.text = $"Player Id: {ctx.BeamContext.PlayerId}";
		BackButton.HandleClicked(GoToHome);
		_cloudSavingService = _ctx.Scope.GetService<ICloudSavingService>();

		SaveButton.HandleClicked(SaveByteData);
		LoadButton.HandleClicked(LoadByteData);
		ForceUploadButton.HandleClicked(ForceUploadData);
		ForceDownloadButton.HandleClicked(ForceDownloadData);

		ContentData.onValueChanged.AddListener(OnContentChanged);

		UpdateServiceStatus(_cloudSavingService.ServiceStatus);

		return Promise.Success;
	}

	private void OnContentChanged(string value)
	{
		_byteValue = Encoding.ASCII.GetBytes(value);
		ByteDataText.text = BitConverter.ToString(_byteValue);
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
		ContentData.interactable = isInitialized;
	}

	public async void SaveByteData()
	{
		await _cloudSavingService.SaveData(SaveFileName, _byteValue);
	}

	private async void LoadByteData()
	{
		var result = await _cloudSavingService.LoadDataByte(SaveFileName);

		if (result != null)
		{
			ByteDataText.text = BitConverter.ToString(result);
			ContentData.text = Encoding.ASCII.GetString(result);
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
			LoadByteData();
		});
	}
}
