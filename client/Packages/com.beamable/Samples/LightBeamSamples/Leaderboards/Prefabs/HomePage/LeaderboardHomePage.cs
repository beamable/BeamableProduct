
using Beamable.Common;
using Beamable.Common.Leaderboards;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LeaderboardHomePageModel
{
	public LeaderboardRef LeaderboardRef;
	public LeaderboardHomePageViewState viewState;
	public bool useMockData;
}

public enum LeaderboardHomePageViewState
{
	TopScores,
	Friends,
	Nearby
}

public class LeaderboardHomePage : MonoBehaviour, ILightComponent<LeaderboardHomePageModel>
{
	[Header("Scene References")]
	public RectTransform scoreContainer;

	public Button topScoreButton;
	public Button friendScoreButton;
	public Button nearbyScoreButton;
	
	public async Promise OnInstantiated(LightBeam beam, LeaderboardHomePageModel model)
	{
		var leaderboards = beam.BeamContext.Leaderboards;
		var leaderboard = leaderboards.GetBoard(model.LeaderboardRef);

		topScoreButton.HandleClicked(async () =>
		{
			await ChangeViewState(beam, model, LeaderboardHomePageViewState.TopScores);
		});
		friendScoreButton.HandleClicked(async () =>
		{
			await ChangeViewState(beam, model, LeaderboardHomePageViewState.Friends);
		});
		nearbyScoreButton.HandleClicked(async () =>
		{
			await ChangeViewState(beam, model, LeaderboardHomePageViewState.Nearby);
		});
		
		switch (model.viewState)
		{
			case LeaderboardHomePageViewState.TopScores:
				await RenderScores(beam, leaderboard.TopScores.LoadCount(25));
				break;
			case LeaderboardHomePageViewState.Friends:
				await RenderScores(beam, leaderboard.FriendScores.LoadCount(25));
				break;
			case LeaderboardHomePageViewState.Nearby:
				await RenderScores(beam, leaderboard.NearbyScores.LoadCount(25));
				break;
		}
	}

	async Promise RenderScores(LightBeam beam, PlayerScoreList list)
	{
		await list.Refresh();
		scoreContainer.Clear();
		var loadingEntries = new List<Promise<EntryDisplayBehaviour>>();
		foreach (var score in list)
		{
			var loadEntry = beam.Instantiate<EntryDisplayBehaviour, PlayerLeaderboardEntry>(scoreContainer, score);
			loadingEntries.Add(loadEntry);
		}

		await Promise.Sequence(loadingEntries);
	}

	async Promise ChangeViewState(LightBeam beam,
	                              LeaderboardHomePageModel model,
	                              LeaderboardHomePageViewState nextState)
	{
		await beam.GotoPage<LeaderboardHomePage, LeaderboardHomePageModel>(new LeaderboardHomePageModel
		{
			LeaderboardRef = model.LeaderboardRef,
			viewState = nextState
		});
	}
}

