
using Beamable;
using Beamable.Common.Leaderboards;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
	[Header("Configuration")]
	public LeaderboardHomePageModel model;

	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public LeaderboardHomePage homePageTemplate;
	public EntryDisplayBehaviour entryDisplayTemplate;
	
	async void Start()
	{
		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddLightComponent<LeaderboardHomePage, LeaderboardHomePageModel>(homePageTemplate);
			builder.AddLightComponent<EntryDisplayBehaviour, PlayerLeaderboardEntry>(entryDisplayTemplate);
		});

		await lightBeam.Scope.Start<LeaderboardHomePage, LeaderboardHomePageModel>(model);
	}
}

