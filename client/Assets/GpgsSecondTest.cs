// using Beamable.Platform.SDK.Auth;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class GpgsSecondTest : MonoBehaviour
{
	async void Start()
	{
		var beamAPI = Beamable.BeamContext.Default;
		await beamAPI.OnReady;
		// var GPGS = new SignInWithGPG();
		await Task.Delay(TimeSpan.FromSeconds(2f));
		// GooglePlayGames.OurUtils.Logger.DebugLogEnabled = true;
		// GPGS.Login();
	}
}
