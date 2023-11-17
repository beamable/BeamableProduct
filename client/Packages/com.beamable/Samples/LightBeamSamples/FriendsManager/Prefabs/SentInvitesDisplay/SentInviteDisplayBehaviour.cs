using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SentInviteDisplayBehaviour : MonoBehaviour, ILightComponent<SentFriendInvite>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerIdLabel;
	public Button cancelButton;

	public Promise OnInstantiated(LightBeam beam, SentFriendInvite model)
	{
		playerIdLabel.text = $"Player Id: {model.invitedPlayerId}";

		cancelButton.HandleClicked(async () =>
		{
			await model.Cancel();
		});

		return Promise.Success;
	}
}
