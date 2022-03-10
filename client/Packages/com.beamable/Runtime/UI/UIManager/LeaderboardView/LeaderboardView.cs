using System;
using System.Collections.Generic;
using Beamable;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Dependencies;
using Beamable.Common.Leaderboards;
using Beamable.Modules.Generics;
using Beamable.Modules.Leaderboards;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// This is an example <see cref="IBeamableView"/>. It is based off the <see cref="LeaderboardsPresenter"/>.
/// </summary>
[BeamContextSystem]
public class LeaderboardView : MonoBehaviour, IAsyncBeamableView
{
    [RegisterBeamableDependencies(int.MinValue)]
    public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
    {
        builder.SetupUnderlyingSystemSingleton<BeamableDefaultLeaderboardSystem, ILeaderboardDeps>();
    }
    
    public interface ILeaderboardDeps : IBeamableViewDeps
    {
        IReadOnlyList<long> Ranks { get; }
        IReadOnlyList<double> Scores { get; }
        IReadOnlyList<string> Aliases { get; }
        IReadOnlyList<Sprite> Avatars { get; }

        public int CurrentUserIndexInLeaderboard { get; }

        public bool TestMode { get; set; }

        Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<ILeaderboardDeps> onComplete = null);
    }
    
    
    [Header("Feature Configuration")]
    public LeaderboardViewFeatureConfig FeatureConfig;
    public LeaderboardViewFeatureConfig BackUpFeatureConfig;
    public int EnrichOrder;

    [Header("UI Components")]
    public GenericButton BackButton;
    public GenericButton TopButton;
    public LeaderboardsRankEntriesPresenter RankEntries;
    public LeaderboardsRankEntryPresenter CurrentUserRankEntry;
    
    [Header("Exposed Events"), Space] public UnityEvent BackButtonAction;

    public BeamableViewGroup.PlayerCountMode SupportedMode => BeamableViewGroup.PlayerCountMode.SinglePlayerUI;
    public int GetEnrichOrder() => EnrichOrder;

    public async Promise EnrichWithContext(BeamContext currentContext)
    {
        if (BackButton != null)
        {
            BackButton.onClick.RemoveAllListeners();
            BackButton.onClick.AddListener(() => BackButtonAction.Invoke());
        }

        if (TopButton != null)
        {
            TopButton.onClick.RemoveAllListeners();
            TopButton.onClick.AddListener(async () =>
            {
                (FeatureConfig, BackUpFeatureConfig) = (BackUpFeatureConfig, FeatureConfig);
                await EnrichWithContext(currentContext);
            });
        }

        var system = currentContext.ServiceProvider.GetService<ILeaderboardDeps>();

        if (CurrentUserRankEntry != null)
        {
            CurrentUserRankEntry.LoadingIndicator.ToggleLoadingIndicator(true);
            RankEntries.ClearPooledRankedEntries();
        }
        
        var testMode = FeatureConfig.TestMode;
        var leaderboardRef = FeatureConfig.LeaderboardRef;
        var entriesAmount = FeatureConfig.EntriesAmount;
        system.TestMode = testMode;

        var beforePlayerScore = system.CurrentUserIndexInLeaderboard >= 0 ? system.Scores[system.CurrentUserIndexInLeaderboard] : -1;
        Debug.Log($"BEFORE ----- {currentContext.PlayerId} - {beforePlayerScore}");
        await system.UpdateState(leaderboardRef, 0, entriesAmount);
        
        var afterPlayerScore = system.CurrentUserIndexInLeaderboard >= 0 ? system.Scores[system.CurrentUserIndexInLeaderboard] : -1;
        Debug.Log($"AFTER ---- {currentContext.PlayerId} - {afterPlayerScore}");

        if (CurrentUserRankEntry != null)
        {
            var userIdx = system.CurrentUserIndexInLeaderboard;
            
            RankEntries.Enrich(system.Aliases, system.Avatars, system.Ranks, system.Scores, system.Ranks[userIdx]);
            RankEntries.RebuildPooledRankEntries();

            CurrentUserRankEntry.Enrich(system.Aliases[userIdx], system.Avatars[userIdx], system.Ranks[userIdx], system.Scores[userIdx], system.Ranks[userIdx]);
            CurrentUserRankEntry.RebuildRankEntry();
        }
    }

    public Promise EnrichWithContext(BeamContext currentContext, int playerIndex)
    {
        throw new NotSupportedException($"This UI does not know how to render multiple players. You can know that by looking at {SupportedMode}.");
    }
    
    
    
}
