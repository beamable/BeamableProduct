using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;

public class LobbyDisplayBehaviour : MonoBehaviour, ILightComponent<Lobby>
{
	[Header("Scene References")]
	public TextMeshProUGUI lobbyNameLabel;
	
	public Promise OnInstantiated(LightBeam beam, Lobby model)
	{
		lobbyNameLabel.text = model.name;
		
		return Promise.Success;
	}
}
