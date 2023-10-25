using Beamable;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public InventoryExampleConfig config;

	async void Start()
	{
		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddSingleton(config);

			builder.AddLightComponent(config.homePage);
			builder.AddLightComponent<ItemDisplayBehaviour, PlayerItem>(config.itemDisplay);
			builder.AddLightComponent<CurrencyDisplayBehaviour, PlayerCurrency>(config.currencyDisplay);
		});
		
		await lightBeam.Scope.Start<HomePage>();
	}
}
