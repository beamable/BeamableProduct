using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinLobbyDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerLobby>
{
	[Header("Scene References")]
	public TMP_InputField lobbyIdInput;
	public Button backButton;
	public Button joinButton;

	private PlayerLobby _lobby;
	private LightBeam _beam;

	public Promise OnInstantiated(LightBeam beam, PlayerLobby model)
	{
		_lobby = model;
		_beam = beam;
		
		backButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});
		
		joinButton.HandleClicked(JoinLobby);
		
		return Promise.Success;
	}

	private async Promise JoinLobby()
	{
		if (string.IsNullOrEmpty(lobbyIdInput.text))
		{
			Debug.Log("[Lobby] The lobby id should contain a valid string.");
			return;
		}

		try
		{
			await _lobby.Join(lobbyIdInput.text);
			await _beam.GotoPage<HomePage>();
		}
		catch(PromiseException e)
		{
			Debug.Log("[Lobby] Couldn't join the lobby! " + e.Message);
		}
	}
}
