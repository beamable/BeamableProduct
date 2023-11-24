using Beamable;
using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyDisplayBehaviour : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public TMP_InputField nameInput;
	public Toggle closedToggle;
	public Button createLobbyBtn;
	public Button backButton;

	private BeamContext _ctx;
	private LightBeam _lightBeam;
	
	public Promise OnInstantiated(LightBeam beam)
	{
		_ctx = beam.BeamContext;
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
		
		await _ctx.Lobby.Create(nameInput.text, restrictionLevel);
		await _lightBeam.GotoPage<HomePage>();
	}
}
