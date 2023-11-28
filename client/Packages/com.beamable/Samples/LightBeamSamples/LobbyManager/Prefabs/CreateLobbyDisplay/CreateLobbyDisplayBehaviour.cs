using Beamable;
using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerLobby>
{
	[Header("Scene References")]
	public TMP_InputField nameInput;
	public TMP_InputField descriptionInput;
	public Toggle closedToggle;
	public Button createLobbyBtn;
	public Button backButton;

	private PlayerLobby _playerLobby;
	private LightBeam _lightBeam;
	
	public Promise OnInstantiated(LightBeam beam, PlayerLobby model)
	{
		_playerLobby = model;
		_lightBeam = beam;
		
		createLobbyBtn.HandleClicked(async () =>
		{
			await CreateLobby();
		});
		
		backButton.HandleClicked(async () =>
		{
			await _lightBeam.GotoPage<HomePage>();
		});
		
		return Promise.Success;
	}

	private async Promise CreateLobby()
	{
		if (string.IsNullOrEmpty(nameInput.text))
		{
			Debug.Log("[Lobby Manager] Lobby Name must not be null or empty.");
			return;
		}

		LobbyRestriction restrictionLevel = closedToggle.isOn ? LobbyRestriction.Closed : LobbyRestriction.Open;
		
		await _playerLobby.Create(nameInput.text, restrictionLevel, description: descriptionInput.text);
		await _lightBeam.GotoPage<HomePage>();
	}
}
