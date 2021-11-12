using System;
using System.Collections.Generic;
using Beamable.Api.Leaderboard;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Leaderboards;
using Beamable.Modules.Generics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Beamable.UI.Leaderboards
{
    public class LeaderboardsModel : Model
    {
        public IBeamableAPI API { get; private set; }
        public LeaderboardService LeaderboardService { get; private set; }
        public LeaderBoardView CurrentLeaderboardView { get; private set; }
        public List<RankEntry> CurrentRankEntries { get; private set; } = new List<RankEntry>();
        public LeaderboardRef LeaderboardRef { get; private set; }
        public RankEntry CurrentUserRankEntry { get; private set; }
        public int EntriesPerPage { get; private set; }
        public int FirstEntryId { get; private set; }
        public int LastEntryId => FirstEntryId + EntriesPerPage;
        private long Dbid { get; set; }

        public override async void Initialize(params object[] initParams)
        {
            LeaderboardRef = (LeaderboardRef) initParams[0];
            EntriesPerPage = (int) initParams[1];
            FirstEntryId = 0;

            API = await Beamable.API.Instance;
            Dbid = API.User.id;

            LeaderboardService = API.LeaderboardService;
            await LeaderboardService.GetUser(LeaderboardRef, Dbid).Then(rankEntry =>
            {
                CurrentUserRankEntry = rankEntry;
            });

            // Debug
            // await Debug_AddEntries();

            await LeaderboardService.GetBoard(LeaderboardRef, FirstEntryId, LastEntryId).Then(OnLeaderboardReceived);
        }

        public async void NextPageClicked()
        {
            OnRefreshRequested?.Invoke();
            FirstEntryId += EntriesPerPage;
            await LeaderboardService.GetBoard(LeaderboardRef, FirstEntryId, LastEntryId).Then(OnLeaderboardReceived);
        }

        public async void PreviousPageClicked()
        {
            if (FirstEntryId == 0)
            {
                return;
            }

            OnRefreshRequested?.Invoke();
            FirstEntryId -= EntriesPerPage;
            FirstEntryId = Mathf.Clamp(FirstEntryId, 0, Int32.MaxValue);
            await LeaderboardService.GetBoard(LeaderboardRef, FirstEntryId, LastEntryId).Then(OnLeaderboardReceived);
        }

        private void OnLeaderboardReceived(LeaderBoardView leaderboardView)
        {
            CurrentLeaderboardView = leaderboardView;
            // CurrentRankEntries = CurrentLeaderboardView.ToList();
            
            // Debug
            CurrentRankEntries = GenerateFakeData(FirstEntryId, LastEntryId - FirstEntryId);
            
            OnRefresh?.Invoke();
        }

        private Promise<EmptyResponse> Debug_AddEntries()
        {
            return LeaderboardService.SetScore(LeaderboardRef, 200, new Dictionary<string, object>
            {
                {"name", "Kharlos"}
            });
        }

        private List<RankEntry> GenerateFakeData(int firstId, int amount)
        {
            List<RankEntry> entries = new List<RankEntry>();

            for (int i = 0; i < amount; i++)
            {
                RankEntryStat[] stats =
                {
                    new RankEntryStat {name = "name", value = $"PlayerName {firstId + i}"}
                };

                entries.Add(new RankEntry {rank = firstId + i, score = Random.Range(1, 10000), stats = stats});
            }

            return entries;
        }
    }
}