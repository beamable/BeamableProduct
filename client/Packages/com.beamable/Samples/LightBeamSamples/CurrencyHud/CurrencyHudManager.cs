
using Beamable.Common.Inventory;
using Beamable.Player;
using Beamable.Runtime.LightBeam;
using System;
using UnityEngine;

public class CurrencyHudManager : MonoBehaviour
{
	[Header("Configuration")]
	public CurrencyRef currencyRef;

	[Header("Scene References")]
	public RectTransform container;

	[Header("Asset References")]
	public CurrencyViewBehaviour currencyViewTemplate;

	// public CanvasGroup loading;
	
	private async void Start()
	{
		var context = await this.InitLightBeams(null, null, builder =>
		{
			builder.AddLightComponent<CurrencyViewBehaviour, PlayerCurrency>(currencyViewTemplate);
		});

		var currency = context.BeamContext.Inventory.GetCurrency(currencyRef);
		await context.SetLightComponent<CurrencyViewBehaviour, PlayerCurrency>(container, currency);
	}
}

