using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceivedInviteDisplayBehaviour : MonoBehaviour, ILightComponent<ReceivedFriendInvite>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerIdLabel;
	public Button acceptButton;
	public Button blockButton;
	
	public Promise OnInstantiated(LightBeam beam, ReceivedFriendInvite model)
	{
		playerIdLabel.text = model.invitingPlayerId.ToString();
		
		acceptButton.HandleClicked(async () =>
		{
			await model.AcceptInvite();
		});
		
		blockButton.HandleClicked(async () =>
		{
			await model.BlockSender();
		});
		
		return Promise.Success;
	}
}
