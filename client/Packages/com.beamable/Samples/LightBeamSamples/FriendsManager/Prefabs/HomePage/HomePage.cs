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
		foreach (string playerName in playersNames)
		{
			var context = BeamContext.ForPlayer(playerName);
			await context.OnReady;
			var model = new FriendsDisplayModel() {playerId = context.PlayerId, social = context.Social};
			PlayerFriendsBehaviour display = await ctx.Instantiate<PlayerFriendsBehaviour, FriendsDisplayModel>(displaysContainer, model);
		}
	}
}
