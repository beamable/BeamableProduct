using System.Collections.Generic;
using Beamable.Common.Api.Leaderboards;
using Beamable.UI.Scripts;
using UnityEngine;

public class LeaderboardsRankEntriesPresenter : MonoBehaviour, PoolableScrollView.IContentProvider
{
    public LeaderboardsRankEntryPresenter LeaderboardsRankEntryPresenterPrefab;
    public PoolableScrollView PoolableScrollView;
    public NewLoadingIndicator LoadingIndicator;

    private readonly List<LeaderboardsRankEntryPresenter> _spawnedEntries = new List<LeaderboardsRankEntryPresenter>();
    private long _currentPlayerRank;
    private List<RankEntry> _renderingRankEntries;

    public void Enrich(List<RankEntry> data, long currentPlayerRank)
    {
        _renderingRankEntries = data;
        _currentPlayerRank = currentPlayerRank;
        PoolableScrollView.SetContentProvider(this);
    }

    public void ClearPooledRankedEntries()
    {
        LoadingIndicator.ToggleLoadingIndicator(true);

        foreach (LeaderboardsRankEntryPresenter entryPresenter in _spawnedEntries)
        {
            Destroy(entryPresenter.gameObject);
        }

        _spawnedEntries.Clear();
    }

    public void RebuildPooledRankEntries()
    {
        var items = new List<PoolableScrollView.IItem>();
        foreach (RankEntry rankEntry in _renderingRankEntries)
        {
            LeaderboardsRankEntryPresenter.PoolData rankEntryPoolData =
                new LeaderboardsRankEntryPresenter.PoolData { RankEntry = rankEntry, Height = 50.0f };
            items.Add(rankEntryPoolData);
        }

        PoolableScrollView.SetContent(items);
        LoadingIndicator.ToggleLoadingIndicator(false);
    }

    public void ScrollToTop()
    {
        PoolableScrollView.Velocity = 0.0f;
        PoolableScrollView.SetPosition(0.0f);
    }

    RectTransform PoolableScrollView.IContentProvider.Spawn(PoolableScrollView.IItem item, out int order)
    {
        // TODO: implement object pooling
        LeaderboardsRankEntryPresenter spawned = Instantiate(LeaderboardsRankEntryPresenterPrefab);
        _spawnedEntries.Add(spawned);
        order = -1;

        if (item is LeaderboardsRankEntryPresenter.PoolData data)
        {
            spawned.Enrich(data.RankEntry, _currentPlayerRank);
            spawned.RebuildRankEntry();
        }

        return spawned.GetComponent<RectTransform>();
    }

    void PoolableScrollView.IContentProvider.Despawn(PoolableScrollView.IItem item, RectTransform rt)
    {
        if (rt == null) return;
            
        // TODO: implement object pooling
        LeaderboardsRankEntryPresenter rankEntryPresenter = rt.GetComponent<LeaderboardsRankEntryPresenter>();
        _spawnedEntries.Remove(rankEntryPresenter);
        Destroy(rankEntryPresenter.gameObject);
    }
}
