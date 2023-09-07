
using Beamable.Common.Inventory;
using Beamable.Runtime.LightBeam;
using System;
using UnityEngine;

public class CurrencyHudManager : MonoBehaviour
{
	[Header("Configuration")]
	public CurrencyRef currencyRef;

	[Header("Scene References")]
	public RectTransform root;

	public CanvasGroup loading;
	
	private async void Start()
	{
		var context = await this.InitLightBeams(root, loading, builder =>
		{
			
		});
	}
}

