
using Beamable;
using Beamable.Common;
using Beamable.Runtime.LightBeam;
using UnityEngine;

public class ConsoleBehaviour : MonoBehaviour, ILightComponent
{
	public Promise OnInstantiated(BeamContext context)
	{
		return Promise.Success;
	}
}

