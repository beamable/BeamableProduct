using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

public class LobbyDetailsDisplayBehaviour : MonoBehaviour, ILightComponent<Lobby>
{
	[Header("Scene References")]
	public TextMeshProUGUI lobbyName;
	public TextMeshProUGUI lobbyId;
	public Button lobbyIdCopyButton;
	public TextMeshProUGUI lobbyDescription;
	public TextMeshProUGUI lobbyRestriction;
	public TextMeshProUGUI lobbyHost;
	public TextMeshProUGUI lobbyPasscode;
	public Transform playersListContainer;
	public Button backButton;
	
	public async Promise OnInstantiated(LightBeam beam, Lobby model)
	{
		lobbyName.text = $"Lobby Name: {model.name}";
		lobbyId.text = $"Lobby Id: {model.lobbyId}";
		lobbyDescription.text = model.description;
		// lobbyRestriction.text = model.restriction;
		// lobbyHost.text = model.host;
		// lobbyPasscode.text = model.passcode;
		
		playersListContainer.Clear();

		//TODO: optmize this
		foreach (LobbyPlayer lobbyPlayer in model.players)
		{
			await beam.Instantiate<PlayerIdDisplayBehaviour, LobbyPlayer>(playersListContainer, lobbyPlayer);
		}
		
		lobbyIdCopyButton.HandleClicked(() =>
		{
#if UNITY_EDITOR
			EditorGUIUtility.systemCopyBuffer = model.lobbyId;
#else
			GUIUtility.systemCopyBuffer = model.lobbyId;
#endif
		});
		
		backButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});
	}
}
