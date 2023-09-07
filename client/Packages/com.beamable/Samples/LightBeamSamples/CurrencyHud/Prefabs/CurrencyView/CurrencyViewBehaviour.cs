
using Beamable.Common;
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
	private PlayerCurrency _model;

	public async Promise OnInstantiated(LightContext context, PlayerCurrency model)
	{
		_model = model;
		model.OnUpdated += Refresh;
		Refresh();
		iconImage.sprite = await model.Content.icon.LoadSprite();
	}

	private void OnDestroy()
	{
		_model.OnUpdated -= Refresh;
	}

	void Refresh()
	{
		valueText.text = _model.Amount.ToString();
	}
}

