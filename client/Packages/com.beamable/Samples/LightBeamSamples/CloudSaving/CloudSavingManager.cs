using Beamable;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
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

		});

		await lightBeam.Scope.Start<HomePage>();
	}
}
