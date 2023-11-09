using Beamable.Common;
using Beamable.Runtime.LightBeams;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PropertyDisplayBehaviour : MonoBehaviour, ILightComponent<PropertyDisplayData>
{
	[Header("Scene references")]
	public TextMeshProUGUI key;
	public TextMeshProUGUI value;
	
	public Promise OnInstantiated(LightBeam beam, PropertyDisplayData model)
	{
		key.text = $"Key: {model.Key}";
		value.text = $"Value: {model.Value}";
		
		return Promise.Success;
	}
}
