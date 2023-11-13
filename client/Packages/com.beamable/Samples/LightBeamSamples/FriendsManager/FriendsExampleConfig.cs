using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Beamable/Examples/Friends Example Config")]
public class FriendsExampleConfig : ScriptableObject
{
	public HomePage homePage;
	public PlayerFriendsBehaviour playerFriendsBehaviour;
	public FriendDisplayBehaviour friendBehaviour;
	public ReceivedInviteDisplayBehaviour receivedInviteBehaviour;
	public BlockedPlayersDisplayBehaviour blockedPlayerBehaviour;
	public SentInviteDisplayBehaviour sentInviteBehaviour;
}
