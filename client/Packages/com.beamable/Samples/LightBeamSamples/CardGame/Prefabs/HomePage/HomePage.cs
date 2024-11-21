using Beamable;
using Beamable.Common;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using UnityEngine;

public class HomePage : MonoBehaviour, ILightComponent
{
	public int playersAmount = 2;
	public Transform playersRoot;

	public async Promise OnInstantiated(LightBeam beam)
	{
		var promises = new List<Promise<PlayerDisplay>>();

		for (int i = 0; i < playersAmount; i++)
		{
			var context = BeamContext.ForPlayer(i.ToString());
			await context.OnReady;
			var model = new PlayerDisplayModel() { playerId = context.PlayerId, social = context.Social };
			Promise<PlayerDisplay> p = beam.Instantiate<PlayerDisplay, PlayerDisplayModel>(playersRoot, model);
			promises.Add(p);
		}

		var sequence = Promise.Sequence(promises);
		await sequence;
	}
}
