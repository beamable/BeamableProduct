using System;
using System.Collections.Generic;
using Beamable.Api.Leaderboard;
using Beamable.Common.Api.Leaderboards;
using Beamable.Modules.Generics;
using UnityEngine;

namespace Beamable.UI.Leaderboards
{
    public class LeaderboardsModel : Model
    {
        public IBeamableAPI API { get; private set; }
        public LeaderboardService LeaderboardService { get; private set; }
        public LeaderBoardView CurrentLeaderboardView { get; private set; }
        public Dictionary<long, RankEntry> CurrentRankEntries { get; private set; } = new Dictionary<long, RankEntry>();
        public string LeaderboardId { get; private set; }
        public int EntriesPerPage { get; private set; }
        public int FirstEntryId { get; private set; }
        public int LastEntryId => FirstEntryId + EntriesPerPage;

        public override async void Initialize(params object[] initParams)
        {
            LeaderboardId = (string) initParams[0];
            EntriesPerPage = (int) initParams[1];
            FirstEntryId = 0;

            API = await Beamable.API.Instance;

            LeaderboardService = API.LeaderboardService;
            await LeaderboardService.GetBoard(LeaderboardId, FirstEntryId, LastEntryId).Then(leaderboardView =>
            {
                CurrentLeaderboardView = leaderboardView;
                OnInitialized?.Invoke();
            });
        }

        public async void NextPage()
        {
            FirstEntryId += LastEntryId;

            await LeaderboardService.GetBoard(LeaderboardId, FirstEntryId, LastEntryId).Then(leaderboardView =>
            {
                CurrentLeaderboardView = leaderboardView;
                CurrentRankEntries = CurrentLeaderboardView.ToDictionary();
                OnChange?.Invoke();
            });
        }

        public async void PreviousPage()
        {
            FirstEntryId -= LastEntryId;
            FirstEntryId = Mathf.Clamp(FirstEntryId, 0, Int32.MaxValue);

            await LeaderboardService.GetBoard(LeaderboardId, FirstEntryId, LastEntryId).Then(leaderboardView =>
            {
                CurrentLeaderboardView = leaderboardView;
                CurrentRankEntries = CurrentLeaderboardView.ToDictionary();
                OnChange?.Invoke();
            });
        }
    }
}