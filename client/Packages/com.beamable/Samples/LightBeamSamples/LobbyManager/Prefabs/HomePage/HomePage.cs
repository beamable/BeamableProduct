using Beamable;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerLobbyData
{
	public PlayerLobby playerLobby;
	public long playerId;
	public string playerName;
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

		List<Promise<Unit>> promises = new List<Promise<Unit>>();
		foreach (string playerName in playersNames)
		{
			Promise p = CreatePlayerLobby(playerName);
			promises.Add(p);
		}
		Promise<List<Unit>> sequence = Promise.Sequence(promises);
		await sequence;
		SortChildrenByName(displaysContainer);
	}

	private async Promise CreatePlayerLobby(string playerName)
	{
		BeamContext context = BeamContext.ForPlayer(playerName);
		await context.OnReady;
		var data = new PlayerLobbyData() {playerLobby = context.Lobby, playerId = context.PlayerId, playerName = context.PlayerCode};
		await _beam.Instantiate<PlayerLobbyBehaviour, PlayerLobbyData>(displaysContainer, data);
	}
	
	private void SortChildrenByName(Transform container) {
		List<Transform> children = new List<Transform>();
		for (int i = container.childCount - 1; i >= 0; i--) {
			Transform child = container.GetChild(i);
			children.Add(child);
			child.SetParent(null);
		}
		
		children.Sort((Transform t1, Transform t2) =>
		{
			var p1 = t1.GetComponent<PlayerLobbyBehaviour>();
			var p2 = t2.GetComponent<PlayerLobbyBehaviour>();
			return string.Compare(p1.gamerTagLabel.text, p2.gamerTagLabel.text, StringComparison.Ordinal);
		});
		
		foreach (Transform child in children) {
			child.SetParent(container);
		}
	}
}
