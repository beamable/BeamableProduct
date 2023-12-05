using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FindLobbyDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerLobby>
{
	[Header("Scene References")]
	public Transform lobbiesContainer;
	public Button backButton;

	public async Promise OnInstantiated(LightBeam beam, PlayerLobby model)
	{
		LobbyQueryResponse response = await model.FindLobbies();

		lobbiesContainer.Clear();
		var promises = new List<Promise<LobbyDisplayBehaviour>>();
		foreach (Lobby lobby in response.results)
		{
			Promise<LobbyDisplayBehaviour> p = beam.Instantiate<LobbyDisplayBehaviour, Lobby>(lobbiesContainer, lobby);
			promises.Add(p);
		}

		Promise<List<LobbyDisplayBehaviour>> sequence = Promise.Sequence(promises);
		await sequence;

		backButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});

	}
}
