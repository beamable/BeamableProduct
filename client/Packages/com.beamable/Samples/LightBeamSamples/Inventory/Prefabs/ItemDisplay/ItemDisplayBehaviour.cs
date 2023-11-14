using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerItem>
{
	[Header("Scene References")]
	public TextMeshProUGUI typeText;
	public TextMeshProUGUI idText;
	public Image icon;
	public Button infoButton;

	private PlayerItem _model;
	private LightBeam _ctx;

	public Promise OnInstantiated(LightBeam beam, PlayerItem model)
	{
		_ctx = beam;
		_model = model;

		model.OnUpdated += Refresh;
		Refresh();

		infoButton.HandleClicked(() =>
		{
			beam.GotoPage<ItemInfoPage, PlayerItem>(model);
		});

		return Promise.Success;
	}

	private void Refresh()
	{
		typeText.text = $"Name: {_model.Content.name}";
		idText.text = $"Id: {_model.Content.Id}";

		icon.sprite = null;
		if (_model.Content.icon != null && _model.Content.icon.Asset != null)
		{
			_model.Content.icon.LoadSprite().Then((sprite) =>
			{
				icon.sprite = sprite;
			});
		}
	}
}
