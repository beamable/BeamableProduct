using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
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
	public TextMeshProUGUI lobbyHost;
	public TextMeshProUGUI lobbyPasscode;
	public Button lobbyPasscodeCopyButton;
	public Transform playersListContainer;
	public Button backButton;

	private Lobby _model;
	private LightBeam _beam;
	
	public async Promise OnInstantiated(LightBeam beam, Lobby model)
	{
		_beam = beam;
		_model = model;
		
		lobbyName.text = $"Lobby Name: {model.name}";
		lobbyId.text = $"Lobby Id: {model.lobbyId}";
		lobbyDescription.text = model.description;
		lobbyPasscode.text = $"Passcode: {model.passcode}";
		lobbyHost.text = $"Host: {model.host}";
		
		playersListContainer.Clear();

		await InstantiatePlayers();
		
		lobbyIdCopyButton.HandleClicked(() =>
		{
#if UNITY_EDITOR
			EditorGUIUtility.systemCopyBuffer = model.lobbyId;
#else
			GUIUtility.systemCopyBuffer = model.lobbyId;
#endif
		});
		
		lobbyPasscodeCopyButton.HandleClicked(() =>
		{
#if UNITY_EDITOR
			EditorGUIUtility.systemCopyBuffer = model.passcode;
#else
			GUIUtility.systemCopyBuffer = model.passcode;
#endif
		});
		
		backButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});
	}

	private async Promise InstantiatePlayers()
	{
		var promises = new List<Promise<PlayerIdDisplayBehaviour>>();
		foreach (LobbyPlayer lobbyPlayer in _model.players)
		{
			Promise<PlayerIdDisplayBehaviour> p = _beam.Instantiate<PlayerIdDisplayBehaviour, LobbyPlayer>(playersListContainer, lobbyPlayer);
			promises.Add(p);
		}
		Promise<List<PlayerIdDisplayBehaviour>> sequence = Promise.Sequence(promises);
		await sequence;
	}
}
