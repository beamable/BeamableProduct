using UnityEngine;

[CreateAssetMenu(menuName = "Beamable/Examples/Lobby Example Config")]
public class LobbyExampleConfig : ScriptableObject
{
	public HomePage homePage;
	public CreateLobbyDisplayBehaviour createLobbyDisplay;
	public FindLobbyDisplayBehaviour findLobbyDislay;
	public LobbyDisplayBehaviour lobbyDisplay;
	public LobbyDetailsDisplayBehaviour lobbyDetailsDisplay;
	public PlayerIdDisplayBehaviour playerIdDisplay;
	public PlayerLobbyBehaviour playerLobby;
	public JoinLobbyDisplayBehaviour joinLobbyDisplay;
}
