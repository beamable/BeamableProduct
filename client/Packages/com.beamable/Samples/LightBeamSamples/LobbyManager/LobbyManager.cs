using Beamable;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Runtime.LightBeams;
using UnityEngine;

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
			builder.AddLightComponent(config.createLobbyDisplay);
			builder.AddLightComponent(config.findLobbyDislay);
			builder.AddLightComponent<LobbyDisplayBehaviour, Lobby>(config.lobbyDisplay);
			builder.AddLightComponent<LobbyDetailsDisplayBehaviour, Lobby>(config.lobbyDetailsDisplay);
			builder.AddLightComponent<PlayerIdDisplayBehaviour, LobbyPlayer>(config.playerIdDisplay);
		});

		await lightBeam.Scope.Start<HomePage>();
	}
}
