
using Beamable;
using Beamable.Runtime.LightBeams;
using UnityEngine;

public class CardGameManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public CardGameConfig config;

	async void Start()
	{
		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddSingleton(config);

			builder.AddLightComponent(config.homePage);
			builder.AddLightComponent<PlayerDisplay, PlayerDisplayModel>(config.playerDisplay);
		});

		await lightBeam.Scope.Start<HomePage>();
	}
}
