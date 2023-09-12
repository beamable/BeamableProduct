
using Beamable;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Common.Inventory;
using Beamable.Player;
using Beamable.Runtime.LightBeam;
using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyViewBehaviour : MonoBehaviour, ILightComponent<PlayerCurrency>
{
	[Header("Scene References")]
	public TMP_Text valueText;
	public Image iconImage;
	
	[Header("Runtime data")]
	public PlayerCurrency data;

	public async Promise OnInstantiated(BeamContext context, PlayerCurrency model)
	{
		data = model;
		model.OnUpdated += Refresh;
		Refresh();

		var contentService = context.Content;
		var content = await contentService.GetContent<CurrencyContent>(new CurrencyRef(model.CurrencyId));
		var sprite = await content.icon.LoadSprite() ;
		iconImage.sprite = sprite;
	}

	private void OnDestroy()
	{
		if (data == null) return;
		data.OnUpdated -= Refresh;
	}

	void Refresh()
	{
		valueText.text = data.Amount.ToString();
	}
}

