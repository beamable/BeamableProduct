using Beamable;using Beamable.Player;
using Beamable.Runtime.LightBeams;
using UnityEngine;
using Lobby = Beamable.Experimental.Api.Lobbies.Lobby;
using LobbyPlayer = Beamable.Experimental.Api.Lobbies.LobbyPlayer;

public class LobbyManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public LobbyExampleConfig config;

	async void Start()
	{
		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddSingleton(config);

			builder.AddLightComponent(config.homePage);
			builder.AddLightComponent<CreateLobbyDisplayBehaviour, PlayerLobby>(config.createLobbyDisplay);
			builder.AddLightComponent<FindLobbyDisplayBehaviour, PlayerLobby>(config.findLobbyDislay);
			builder.AddLightComponent<LobbyDisplayBehaviour, Lobby>(config.lobbyDisplay);
			builder.AddLightComponent<LobbyDetailsDisplayBehaviour, Lobby>(config.lobbyDetailsDisplay);
			builder.AddLightComponent<PlayerIdDisplayBehaviour, LobbyPlayer>(config.playerIdDisplay);
			builder.AddLightComponent<PlayerLobbyBehaviour, PlayerLobbyData>(config.playerLobby);
		});

		await lightBeam.Scope.Start<HomePage>();
	}
}
