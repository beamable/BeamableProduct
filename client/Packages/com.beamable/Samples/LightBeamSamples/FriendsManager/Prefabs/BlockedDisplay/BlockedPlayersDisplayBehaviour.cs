using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockedPlayersDisplayBehaviour : MonoBehaviour, ILightComponent<BlockedPlayer>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerIdLabel;
	public Button unblockButton;
	
	public Promise OnInstantiated(LightBeam beam, BlockedPlayer model)
	{
		playerIdLabel.text = $"Player Id: {model.playerId}";
		
		unblockButton.HandleClicked(async () =>
		{
			await model.Unblock();
		});
		
		return Promise.Success;
	}
}
