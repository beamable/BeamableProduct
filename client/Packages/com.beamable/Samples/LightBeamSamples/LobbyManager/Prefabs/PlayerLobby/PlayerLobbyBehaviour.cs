using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyBehaviour : MonoBehaviour, ILightComponent<PlayerLobbyData>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerIdLabel;
	public Button createLobbyBtn;
	public Button findLobbyBtn;
	
	public Promise OnInstantiated(LightBeam beam, PlayerLobbyData model)
	{
		playerIdLabel.text = model.playerId.ToString();
		
		createLobbyBtn.HandleClicked(async () =>
		{
			await beam.GotoPage<CreateLobbyDisplayBehaviour, PlayerLobby>(model.playerLobby);
		});
		
		findLobbyBtn.HandleClicked(async () =>
		{
			await beam.GotoPage<FindLobbyDisplayBehaviour, PlayerLobby>(model.playerLobby);
		});
		
		return Promise.Success;
	}
}
