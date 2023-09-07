
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeam;
using TMPro;
using UnityEngine;

public class CurrencyViewBehaviour : MonoBehaviour, ILightComponent<PlayerCurrency>
{
	public TMP_Text valueText;
	
	public Promise OnInstantiated(LightContext context, PlayerCurrency model)
	{
		
		
		valueText.text = model.Amount.ToString();

		return Promise.Success;
	}
}

