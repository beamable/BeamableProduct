using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using UnityEngine;
using UnityEngine.UI;

public class FindLobbyDisplayBehaviour : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Transform lobbiesContainer;
	public Button backButton;
	
	public async Promise OnInstantiated(LightBeam beam)
	{

		LobbyQueryResponse response = await beam.BeamContext.Lobby.FindLobbies();

		lobbiesContainer.Clear();
		foreach (Lobby lobby in response.results)
		{
			//improve this with sequence
			await beam.Instantiate<LobbyDisplayBehaviour, Lobby>(lobbiesContainer, lobby);
		}
		
		backButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});
		
	}
}
