using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Player.CloudSaving;
using Beamable.Runtime.LightBeams;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[BeamContextSystem]
public class HomePage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Button InitSystemButton;
	public Button StringPageButton;
	public Button BytePageButton;
	public Button JsonPageButton;

	public GameObject ConflictRoot;
	public TextMeshProUGUI ConflictFileName;
	public ConflictViewReferences LocalConflictData;
	public ConflictViewReferences CloudConflictData;

	public TextMeshProUGUI _serviceStatus;
	public TextMeshProUGUI playerId;

	private LightBeam _ctx;
	private ICloudSavingService _cloudSavingService;


	public Promise OnInstantiated(LightBeam ctx)
	{
		_ctx = ctx;
		playerId.text = $"Player Id: {ctx.BeamContext.PlayerId}";
		InitSystemButton.HandleClicked(InitSystem);
		_cloudSavingService = _ctx.Scope.GetService<ICloudSavingService>();

		StringPageButton.HandleClicked(GoToStringPage);
		BytePageButton.HandleClicked(GoToBytePage);
		JsonPageButton.HandleClicked(GoToJsonPage);
		
		UpdateServiceStatus(_cloudSavingService.ServiceStatus);

		_cloudSavingService.SetConflictResolverOverride(CustomConflictResolver);
		
		return Promise.Success;
	}

	private void OnDestroy()
	{
		_cloudSavingService.SetConflictResolverOverride(null);
	}

	private void GoToJsonPage()
	{
		_ctx.GotoPage<JsonPage>();
	}

	private void GoToBytePage()
	{
		_ctx.GotoPage<BytePage>();
	}

	private void GoToStringPage()
	{
		_ctx.GotoPage<StringPage>();
	}

	public void InitSystem()
	{
		_cloudSavingService.Init().Then(status =>
		{
			UpdateServiceStatus(status);
		});
		UpdateServiceStatus(_cloudSavingService.ServiceStatus);
	}

	private void UpdateServiceStatus(CloudSaveStatus serviceStatus)
	{
		_serviceStatus.text = $"Service Status: {serviceStatus}";
		bool isInitialized = serviceStatus == CloudSaveStatus.Initialized;
		StringPageButton.interactable = isInitialized;
		BytePageButton.interactable = isInitialized;
		JsonPageButton.interactable = isInitialized;
		InitSystemButton.interactable = serviceStatus == CloudSaveStatus.Inactive;
	}

	private void CustomConflictResolver(IConflictResolver conflictresolver)
	{
		StartCoroutine(ResolveConflictRoutine(conflictresolver));
	}

	private IEnumerator ResolveConflictRoutine(IConflictResolver conflictResolver)
	{
		ConflictRoot.SetActive(true);
		while (conflictResolver.Conflicts.Count > 0)
		{
			bool isSolving = true;
			var conflict = conflictResolver.Conflicts[0];
			ConflictFileName.text = conflict.FileName;
			LocalConflictData.SetValues(conflict.LocalSaveEntry, () =>
			{
				conflictResolver.Resolve(conflict, ConflictResolveType.UseLocal);
				isSolving = false;
			});
			CloudConflictData.SetValues(conflict.CloudSaveEntry, () =>
			{
				conflictResolver.Resolve(conflict, ConflictResolveType.UseCloud);
				isSolving = false;
			});
			
			yield return new WaitWhile(() => isSolving);
		}
		ConflictRoot.SetActive(false);
		UpdateServiceStatus(_cloudSavingService.ServiceStatus);
		yield return null;
	}
	
	
}
