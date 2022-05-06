using Beamable.Platform.SDK.Auth;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class GpgsSecondTest : MonoBehaviour
{
	async void Start()
	{
		var beamAPI = Beamable.BeamContext.Default;
		await beamAPI.OnReady;
		var GPGS = new SignInWithGPG();
		GPGS.OnRequestServerSideAccessResult += HandleRequestServerSideAccessResult;
		await Task.Delay(TimeSpan.FromSeconds(2f));
		GooglePlayGames.OurUtils.Logger.DebugLogEnabled = true;
		GPGS.Login();
	}

	private void HandleRequestServerSideAccessResult(bool success, string token)
	{
		if(!success)
		{
			throw new Exception("Cannot get server token from GPGS, please check if your configuration is correct.");
		}
		else
		{
			Debug.Log($"SUCCESS: {token}");
		}
	}
}
