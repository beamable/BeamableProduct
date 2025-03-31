using Beamable;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Player;
using Beamable.Player.CloudSaving;
using Beamable.Runtime.LightBeams;
using System;
using UnityEngine;

public class CloudSavingManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;

	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public CloudSavingExampleConfig config;
	
	async void Start()
	{
		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddSingleton(config);

			builder.AddLightComponent(config.homePage);
			builder.AddLightComponent(config.stringPage);
			builder.AddLightComponent(config.bytePage);
			builder.AddLightComponent(config.jsonPage);

		});
		
		await lightBeam.Scope.Start<HomePage>();
	}
}
