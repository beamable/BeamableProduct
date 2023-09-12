
using Beamable;
using Beamable.Common.Inventory;
using Beamable.Player;
using Beamable.Runtime.LightBeam;
using System;
using System.Collections.Generic;
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
		var context = BeamContext.InParent(this);
		await context.CreateLightBeam(container, loading, builder =>
		{
			builder.AddLightComponent<CurrencyViewBehaviour, PlayerCurrency>(currencyViewTemplate);
		});

		var currency = context.Inventory.GetCurrency(currencyRef);
		container.Clear();
		await context.Instantiate<CurrencyViewBehaviour, PlayerCurrency>(container, currency).ShowLoading(context);
		
		infoText.text = $@"PlayerId=[{context.PlayerId}]
The currency shown in the top-right is the currency=[{currencyRef.Id}]";
	}
}

