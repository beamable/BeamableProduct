


using Beamable;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using UnityEngine;

public class LootBoxManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public LootBoxExampleConfig config;

	async void Start()
	{
		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddSingleton(config);

			builder.AddLightComponent(config.lootBoxPage);
		});

		await lightBeam.Scope.Start<LootBoxPage>();
	}
}
