using System.Collections.Generic;
using Beamable.Common.Api.Leaderboards;
using Beamable.Modules.Generics;
using UnityEngine;

namespace Beamable.UI.Leaderboards
{
    public class LeaderboardsRankEntriesPresenter : DataPresenter<List<RankEntry>>
    {
#pragma warning disable CS0649
        [SerializeField] private Transform _content;
        [SerializeField] private LeaderboardsRankEntryPresenter _leaderboardsRankEntryPresenterPrefab;
#pragma warning restore CS0649

        private readonly List<LeaderboardsRankEntryPresenter> _spawnedEntries = new List<LeaderboardsRankEntryPresenter>();

        public override void ClearData()
        {
            base.ClearData();
            
            foreach (LeaderboardsRankEntryPresenter entryPresenter in _spawnedEntries)
            {
                Destroy(entryPresenter.gameObject);
            }
            
            _spawnedEntries.Clear(); 
        }

        protected override void Refresh()
        {
            foreach (RankEntry rankEntry in Data)
            {
                LeaderboardsRankEntryPresenter spawned = Instantiate(_leaderboardsRankEntryPresenterPrefab, _content, false);
                spawned.Setup(rankEntry);
                _spawnedEntries.Add(spawned);
            }
        }
    }
}