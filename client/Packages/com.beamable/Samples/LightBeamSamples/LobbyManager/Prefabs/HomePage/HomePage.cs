using Beamable;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using System.Linq;
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

		List<Promise<Unit>> promises = playersNames.Select(CreatePlayerLobby).Cast<Promise<Unit>>().ToList();

		Promise<List<Unit>> sequence = Promise.Sequence(promises);
		await sequence;
	}

	private async Promise CreatePlayerLobby(string playerName)
	{
		BeamContext context = BeamContext.ForPlayer(playerName);
		await context.OnReady;
		var data = new PlayerLobbyData() {playerLobby = context.Lobby, playerId = context.PlayerId};
		await _beam.Instantiate<PlayerLobbyBehaviour, PlayerLobbyData>(displaysContainer, data);
	}
}
