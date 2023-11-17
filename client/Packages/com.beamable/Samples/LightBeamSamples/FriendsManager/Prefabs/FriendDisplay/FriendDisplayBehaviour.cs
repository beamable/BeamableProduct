using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerFriend>
{
	[Header("Scene References")]
	public TextMeshProUGUI friendIdLabel;
	public Button unfriendButton;
	public Button blockButton;

	public Promise OnInstantiated(LightBeam beam, PlayerFriend model)
	{
		friendIdLabel.text = model.playerId.ToString();

		unfriendButton.HandleClicked(async () =>
		{
			await model.Unfriend();
		});

		blockButton.HandleClicked(async () =>
		{
			await model.Block();
		});

		return Promise.Success;
	}
}
