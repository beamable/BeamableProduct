using Beamable;
using Beamable.Common;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using UnityEngine;

public class HomePage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Transform displaysContainer;
	public List<string> playersNames;
	

	public async Promise OnInstantiated(LightBeam ctx)
	{
		displaysContainer.Clear();
		
		var promises = new List<Promise<PlayerFriendsBehaviour>>();
		
		foreach (string playerName in playersNames)
		{
			var context = BeamContext.ForPlayer(playerName);
			await context.OnReady;
			var model = new FriendsDisplayModel() {playerId = context.PlayerId, social = context.Social};
			Promise<PlayerFriendsBehaviour> p = ctx.Instantiate<PlayerFriendsBehaviour, FriendsDisplayModel>(displaysContainer, model);
			promises.Add(p);
		}
		var sequence = Promise.Sequence(promises);
		await sequence;
	}
}
