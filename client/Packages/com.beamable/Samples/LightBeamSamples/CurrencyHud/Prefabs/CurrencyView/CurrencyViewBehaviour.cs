
using Beamable.Common;
using Beamable.Common.Inventory;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
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

	public async Promise OnInstantiated(LightBeam beam, PlayerCurrency model)
	{
		data = model;
		model.OnUpdated += Refresh;
		Refresh();

		var contentService = beam.BeamContext.Content;
		var content = await contentService.GetContent<CurrencyContent>(new CurrencyRef(model.CurrencyId));

		if (content.icon == null)
		{
			iconImage.sprite = null;
		}
		else
		{
			iconImage.sprite = await content.icon.LoadSprite();
		}
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

