using Beamable;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLobbyData
{
	public PlayerLobby playerLobby;
	public long playerId;
}

public class HomePage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Transform displaysContainer;
	public List<string> playersNames;

	private LightBeam _beam;
	
	public async Promise OnInstantiated(LightBeam beam)
	{
		_beam = beam;
		displaysContainer.Clear();

		var promises = new List<Promise<Unit>>();

		foreach (string playerName in playersNames)
		{
			Promise p = CreatePlayerLobby(playerName);
			promises.Add(p);
		}
		Promise<List<Unit>> sequence = Promise.Sequence(promises);
		await sequence;
	}

	private async Promise CreatePlayerLobby(string playerName)
	{
		BeamContext context = BeamContext.ForPlayer(playerName);
		await context.OnReady;
		PlayerLobbyData data = new PlayerLobbyData() {playerLobby = context.Lobby, playerId = context.PlayerId};
		await _beam.Instantiate<PlayerLobbyBehaviour, PlayerLobbyData>(displaysContainer, data);
	}
}
