
using Beamable;
using Beamable.Common.Inventory;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;

public class CurrencyHudManager : MonoBehaviour
{
	[Header("Configuration")]
	public CurrencyRef currencyRef;

	[Header("Scene References")]
	public RectTransform container;
	public CanvasGroup loading;

	public TMP_Text infoText;

	[Header("Asset References")]
	public CurrencyViewBehaviour currencyViewTemplate;


	private async void Start()
	{
		var beamContext = BeamContext.Default;
		var context = await beamContext.CreateLightBeam(container, loading, builder =>
		{
			builder.AddLightComponent<CurrencyViewBehaviour, PlayerCurrency>(currencyViewTemplate);
		});

		var currency = context.BeamContext.Inventory.GetCurrency(currencyRef);
		container.Clear();
		await context.Instantiate<CurrencyViewBehaviour, PlayerCurrency>(container, currency).ShowLoading(context);

		infoText.text = $@"PlayerId=[{context.BeamContext.PlayerId}]
The currency shown in the top-right is the currency=[{currencyRef.Id}]";
	}
}

