using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDisplayBehaviour : MonoBehaviour, ILightComponent<Lobby>
{
	[Header("Scene References")]
	public TextMeshProUGUI lobbyNameLabel;
	public TextMeshProUGUI lobbyIdLabel;
	public Button enterDetailsButton;
	
	public Promise OnInstantiated(LightBeam beam, Lobby model)
	{
		lobbyNameLabel.text = $"Name: {model.name}";
		lobbyIdLabel.text = $"Id: {model.lobbyId}";
		
		enterDetailsButton.HandleClicked(async () =>
		{
			await beam.GotoPage<LobbyDetailsDisplayBehaviour, Lobby>(model);
		});
		
		return Promise.Success;
	}
}
