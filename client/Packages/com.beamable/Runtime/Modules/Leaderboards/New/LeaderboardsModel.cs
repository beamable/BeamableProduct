using Beamable.AccountManagement;
using System;
using System.Collections.Generic;
using Beamable.Api.Leaderboard;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Leaderboards;
using Beamable.Modules.Generics;
using Beamable.Stats;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.UI.Leaderboards
{
	public class LeaderboardsModel : Model
	{
		public event Action OnScrollRefresh;

		private IBeamableAPI _api;
		private LeaderboardService _leaderboardService;
		private LeaderBoardView _currentLeaderboardView;
		private LeaderboardRef _leaderboardRef;
		private int _entriesPerPage;
		private bool _testMode;
		private long _dbid;

		public List<RankEntry> CurrentRankEntries
		{
			get;
			private set;
		} = new List<RankEntry>();

		public RankEntry CurrentUserRankEntry
		{
			get;
			private set;
		}

		public int FirstEntryId
		{
			get;
			private set;
		}

		public bool Configured
		{
			get;
			private set;
		}

		public StatObject AliasStatObject
		{
			get;
			private set;
		}

		private int LastEntryId => FirstEntryId + _entriesPerPage;

		public override async void Initialize(params object[] initParams)
		{
			_leaderboardRef = (LeaderboardRef)initParams[0];
			_entriesPerPage = (int)initParams[1];
			_entriesPerPage = Mathf.Clamp(_entriesPerPage, 1, Int32.MaxValue);
			_testMode = (bool)initParams[2];

			AliasStatObject = AccountManagementConfiguration.Instance.DisplayNameStat;
			FirstEntryId = 1;

			Assert.IsNotNull(_leaderboardRef, "Leaderboard Ref has not been set");
			Assert.IsNotNull(AliasStatObject, "Display Name Stat in Project Settings/Beamable/Account Management has not been set");

			_api = await Beamable.API.Instance;
			_dbid = _api.User.id;
			_leaderboardService = _api.LeaderboardService;

			await _leaderboardService.GetUser(_leaderboardRef, _dbid).Then(rankEntry =>
			{
				CurrentUserRankEntry = !_testMode
					? rankEntry
					: LeaderboardsModelHelper.GenerateCurrentUserRankEntryTestData(
						AliasStatObject.StatKey, AliasStatObject.DefaultValue);
			});

			await _leaderboardService.GetBoard(_leaderboardRef, FirstEntryId, LastEntryId).Then(OnLeaderboardReceived);
		}

		public async void NextPageClicked()
		{
			if (IsBusy)
			{
				return;
			}

			InvokeRefreshRequested();
			FirstEntryId += _entriesPerPage;
			await _leaderboardService.GetBoard(_leaderboardRef, FirstEntryId, LastEntryId).Then(OnLeaderboardReceived);
		}

		public async void PreviousPageClicked()
		{
			if (IsBusy)
			{
				return;
			}

			if (FirstEntryId <= 1)
			{
				return;
			}

			InvokeRefreshRequested();
			FirstEntryId -= _entriesPerPage;
			FirstEntryId = Mathf.Clamp(FirstEntryId, 1, Int32.MaxValue);
			await _leaderboardService.GetBoard(_leaderboardRef, FirstEntryId, LastEntryId).Then(OnLeaderboardReceived);
		}

		public void ScrollToTopButtonClicked()
		{
			if (IsBusy)
			{
				return;
			}

			OnScrollRefresh?.Invoke();
		}

		private void OnLeaderboardReceived(LeaderBoardView leaderboardView)
		{
			_currentLeaderboardView = leaderboardView;

			CurrentRankEntries = !_testMode
				? _currentLeaderboardView.ToList()
				: LeaderboardsModelHelper.GenerateLeaderboardsTestData(FirstEntryId, LastEntryId, CurrentUserRankEntry,
				                                                       AliasStatObject.StatKey,
				                                                       AliasStatObject.DefaultValue);

			InvokeRefresh();
		}
	}
}
