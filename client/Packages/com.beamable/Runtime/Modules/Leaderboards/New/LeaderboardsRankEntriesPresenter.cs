using System.Collections.Generic;
using Beamable.Common.Api.Leaderboards;
using Beamable.Modules.Generics;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.UI.Leaderboards
{
    public class LeaderboardsRankEntriesPresenter : DataPresenter<List<RankEntry>>
    {
#pragma warning disable CS0649
	    [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private LeaderboardsRankEntryPresenter _leaderboardsRankEntryPresenterPrefab;
#pragma warning restore CS0649

        private readonly List<LeaderboardsRankEntryPresenter> _spawnedEntries = new List<LeaderboardsRankEntryPresenter>();
        private long _currentPlayerRank;

        public override void Setup(List<RankEntry> data, params object[] additionalParams)
        {
	        _currentPlayerRank = (long) additionalParams[0];
	        base.Setup(data, additionalParams);
        }

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
	        ScrollToTop();
	        
            foreach (RankEntry rankEntry in Data)
            {
                LeaderboardsRankEntryPresenter spawned = Instantiate(_leaderboardsRankEntryPresenterPrefab, _content, false);
                spawned.Setup(rankEntry, _currentPlayerRank);
                _spawnedEntries.Add(spawned);
            }
        }

        public void ScrollToTop()
        {
	        _scrollRect.StopMovement();
	        _content.anchoredPosition = Vector2.zero;
        }
    }
}
