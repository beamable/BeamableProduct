using Beamable;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;

public class FriendsDisplayModel
{
	public PlayerSocial social;
	public long playerId;
}

public class PlayerFriendsBehaviour : MonoBehaviour, ILightComponent<FriendsDisplayModel>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerIdLabel;

	private PlayerSocial _social;
	
	public Promise OnInstantiated(LightBeam beam, FriendsDisplayModel model)
	{
		playerIdLabel.text = $"Player Id: {model.playerId}";

		_social = model.social;
		//_social.SentInvites.
		
		return Promise.Success;
	}
}
