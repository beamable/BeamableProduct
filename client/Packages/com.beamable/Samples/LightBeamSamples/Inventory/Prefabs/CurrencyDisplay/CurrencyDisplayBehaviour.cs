using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerCurrency>
{
	[Header("Scene References")]
	public TextMeshProUGUI typeText;
	public TextMeshProUGUI idText;
	public Image icon;
	public Button infoButton;
	
	private PlayerCurrency _model;
	private LightBeam _ctx;

	public Promise OnInstantiated(LightBeam beam, PlayerCurrency model)
	{
		_ctx = beam;
		_model = model;

		model.OnUpdated += Refresh;
		Refresh();
		
		infoButton.HandleClicked(() =>
		{
			beam.GotoPage<CurrencyInfoBehaviour, PlayerCurrency>(model);
		});
	    
		return Promise.Success;
	}

	private void Refresh()
	{
		typeText.text = $"Name: {_model.Content.name}";
		idText.text = $"Amount: {_model.Amount}";

		icon.sprite = null;
		if (_model.Content.icon.Asset != null)
		{
			_model.Content.icon.LoadSprite().Then((sprite) =>
			{
				icon.sprite = sprite;
			});
		}
	}
}
