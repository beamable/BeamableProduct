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
	public TMP_InputField passcodeInput;
	public Button backButton;
	public Button joinButton;

	private PlayerLobby _lobby;
	private LightBeam _beam;

	public Promise OnInstantiated(LightBeam beam, PlayerLobby model)
	{
		_lobby = model;
		_beam = beam;

		lobbyIdInput.onValueChanged.AddListener(OnIdInputChanged);
		passcodeInput.onValueChanged.AddListener(OnPasscodeInputChanged);
		
		backButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});
		
		joinButton.HandleClicked(JoinLobby);
		
		return Promise.Success;
	}

	private async Promise JoinLobby()
	{
		if (string.IsNullOrEmpty(lobbyIdInput.text) && string.IsNullOrEmpty(passcodeInput.text))
		{
			Debug.Log("[Lobby] The lobby id or the passcode should contain a valid string.");
			return;
		}

		try
		{
			if (!string.IsNullOrEmpty(lobbyIdInput.text))
			{
				await _lobby.Join(lobbyIdInput.text);
			}
			else
			{
				await _lobby.JoinByPasscode(passcodeInput.text);
			}
			
			await _beam.GotoPage<HomePage>();
		}
		catch(PromiseException e)
		{
			Debug.Log("[Lobby] Couldn't join the lobby! " + e.Message);
		}
	}

	private void OnIdInputChanged(string updatedValue)
	{
		passcodeInput.text = string.Empty;
		passcodeInput.readOnly = !string.IsNullOrEmpty(updatedValue);
	}

	private void OnPasscodeInputChanged(string updatedValue)
	{
		lobbyIdInput.text = string.Empty;
		lobbyIdInput.readOnly = !string.IsNullOrEmpty(updatedValue);
	}
}
