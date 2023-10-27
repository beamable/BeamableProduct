using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertyDisplayData
{
	public string Key;
	public string Value;
}

public class CurrencyInfoBehaviour : MonoBehaviour, ILightComponent<PlayerCurrency>
{
	[Header("Scene references")]
	public Image currencyIcon;
	public TextMeshProUGUI currencyId;
	public TextMeshProUGUI amount;
	public Transform propertiesContainer;
	public Button backButton;

	private LightBeam _beam;
	private PlayerCurrency _model;
	
	public Promise OnInstantiated(LightBeam beam, PlayerCurrency model)
	{
		_beam = beam;
		_model = model;
		model.OnUpdated += Refresh;
		Refresh();
		
		backButton.HandleClicked(() =>
		{
			beam.GotoPage<HomePage>();
		});
		
		return Promise.Success;
	}

	private void Refresh()
	{
		propertiesContainer.Clear();
		
		if (_model.Content.icon.Asset != null)
		{
			_model.Content.icon.LoadSprite().Then((sprite) =>
			{
				currencyIcon.sprite = sprite;
			});
		}

		currencyId.text = _model.CurrencyId;
		amount.text = $"Amount: {_model.Amount.ToString()}";

		foreach (KeyValuePair<string,string> property in _model.Properties)
		{
			var data = new PropertyDisplayData() {Key = property.Key, Value = property.Value};
			_beam.Instantiate<PropertyDisplayBehaviour, PropertyDisplayData>(propertiesContainer, data);
		}
	}
}
