using System;
using System.Collections.Generic;
using Beamable.Api.Leaderboard;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Leaderboards;
using Beamable.Modules.Generics;
using Beamable.Stats;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Beamable.UI.Leaderboards
{
    public class LeaderboardsModel : Model
    {
        public List<RankEntry> CurrentRankEntries { get; private set; } = new List<RankEntry>();
        public RankEntry CurrentUserRankEntry { get; private set; }
        public int FirstEntryId { get; private set; }

        private IBeamableAPI _api;
        private LeaderboardService _leaderboardService;
        private LeaderBoardView _currentLeaderboardView;
        private LeaderboardRef _leaderboardRef;
        private int _entriesPerPage;
        private StatObject _nameStatObject;
        private bool _testMode;
        private long _dbid;
        
        private int LastEntryId => FirstEntryId + _entriesPerPage;

        public override async void Initialize(params object[] initParams)
        {
            _leaderboardRef = (LeaderboardRef) initParams[0];
            _entriesPerPage = (int) initParams[1];
            _entriesPerPage = Mathf.Clamp(_entriesPerPage, 1, Int32.MaxValue);
            _nameStatObject = (StatObject) initParams[2];
            _testMode = (bool) initParams[3];
            FirstEntryId = 1;

            _api = await Beamable.API.Instance;
            _dbid = _api.User.id;

            _leaderboardService = _api.LeaderboardService;
            await _leaderboardService.GetUser(_leaderboardRef, _dbid).Then(rankEntry =>
            {
                CurrentUserRankEntry = rankEntry;
            });

            if (_testMode)
            {
                await SetTestScore();
            }

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

        private void OnLeaderboardReceived(LeaderBoardView leaderboardView)
        {
            _currentLeaderboardView = leaderboardView;

            CurrentRankEntries = !_testMode
                ? _currentLeaderboardView.ToList()
                : GenerateTestData(FirstEntryId, LastEntryId - FirstEntryId);

            InvokeRefresh();
        }

        #region Test data

        private Promise<EmptyResponse> SetTestScore()
        {
            return _leaderboardService.SetScore(_leaderboardRef, 200, new Dictionary<string, object>
            {
                {_nameStatObject.StatKey, _nameStatObject.DefaultValue}
            });
        }

        private List<RankEntry> GenerateTestData(int firstId, int amount)
        {
            List<RankEntry> entries = new List<RankEntry>();

            for (int i = 0; i < amount; i++)
            {
                RankEntryStat[] stats =
                {
                    new RankEntryStat
                        {name = _nameStatObject.StatKey, value = $"{_nameStatObject.DefaultValue} {firstId + i}"}
                };

                entries.Add(new RankEntry {rank = firstId + i, score = Random.Range(1, 10000), stats = stats});
            }

            return entries;
        }

        #endregion
    }
}