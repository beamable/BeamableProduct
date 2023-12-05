using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;

public class PlayerIdDisplayBehaviour : MonoBehaviour, ILightComponent<LobbyPlayer>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerId;
	public Promise OnInstantiated(LightBeam beam, LobbyPlayer model)
	{
		playerId.text = $"Id: {model.playerId}";
		
		return Promise.Success;
	}
}
