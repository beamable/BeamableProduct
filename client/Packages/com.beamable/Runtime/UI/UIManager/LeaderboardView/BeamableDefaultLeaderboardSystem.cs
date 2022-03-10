using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Leaderboard;
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Dependencies;
using Beamable.Common.Leaderboards;
using Beamable.Modules.Leaderboards;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// This is our basic leaderboard system --- It exposes some APIs to fetch data from the backend and rearranges that API with basic information from our backend into a
/// format more easily usable by UI.
/// It gets <see cref="RankEntry"/>s from our platform, parses them, loads data relevant and caches that data in a format that's easier to work with for the things we want to do. In this case,
/// it simply caches the leaderboards names, avatar sprites, rank and score values in sequential parallel list.
/// </summary>
public class BeamableDefaultLeaderboardSystem : LeaderboardView.ILeaderboardDeps, UserProfileView.IRankDeps
{
    /// <summary>
    /// Whether or not this is using mocked test data.
    /// </summary>
    public bool TestMode { get; set; }

    public IReadOnlyList<string> Aliases { get; set; }
    public IReadOnlyList<Sprite> Avatars { get; set; }
    public IReadOnlyList<long> Ranks { get; set; }
    public IReadOnlyList<double> Scores { get; set; }
    
    public int CurrentUserIndexInLeaderboard { get; private set; }


    private IUserContext _userContext;

    /// <summary>
    /// Reference to our platform's leaderboard API.
    /// </summary>
    private readonly LeaderboardService _leaderboardService;

    /// <summary>
    /// Constructs with the appropriate dependencies. Is injected by <see cref="BeamContext"/> dependency injection framework.
    /// </summary>
    public BeamableDefaultLeaderboardSystem(LeaderboardService leaderboardService, IUserContext ctx)
    {
        _leaderboardService = leaderboardService;
        _userContext = ctx;

        Aliases = new List<string>();
        Avatars = new List<Sprite>();
        Ranks = new List<long>();
        Scores = new List<double>();
        CurrentUserIndexInLeaderboard = -1;
    }


    long UserProfileView.IRankDeps.GetUserRank() => CurrentUserIndexInLeaderboard < 0 ? -1 : Ranks[CurrentUserIndexInLeaderboard];

    /// <summary>
    /// Just a simple API that will go talk to the Back-End, wait for the response and update this system's internal state.
    /// </summary>
    public virtual async Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<LeaderboardView.ILeaderboardDeps> onComplete = null)
    {
        if (TestMode)
        {
            await Task.Delay(1000);
            var testUserEntry = LeaderboardsModelHelper.GenerateCurrentUserRankEntryTestData("_aliasStatObject.StatKey", "100");
            var testEntries = LeaderboardsModelHelper.GenerateLeaderboardsTestData(firstEntryId, firstEntryId + entriesAmount, testUserEntry, "_aliasStatObject.StatKey", "100");
            BuildLeaderboardViewDepsFromRankEntries(testEntries, testUserEntry);
        }
        else
        {
            try
            {
                var userRankEntry = await _leaderboardService.GetUser(_leaderboardRef, _userContext.UserId);
                var leaderBoardView = await _leaderboardService.GetBoard(_leaderboardRef, firstEntryId, firstEntryId + entriesAmount);

                var rankEntries = leaderBoardView.ToList();

                BuildLeaderboardViewDepsFromRankEntries(rankEntries, userRankEntry);
            }
            catch
            {
                var ranks = Enumerable.Range(0, entriesAmount).ToArray();
                Aliases = ranks.Select(r => $"Fake Player At Rank {r}").ToList();
                Avatars = Enumerable.Repeat(GetAvatar(""), entriesAmount).ToList();
                Ranks = ranks.Select(i => (long)i).ToList();
                Scores = ranks.Select(re => (double)UnityEngine.Random.Range(0, re)).ToList();
            }

            onComplete?.Invoke(this);
        }
    }

    /// <summary>
    /// The actual data transformation function that converts rank entries into data that is relevant for our <see cref="LeaderboardView.ILeaderboardDeps"/>. 
    /// </summary>
    private void BuildLeaderboardViewDepsFromRankEntries(List<RankEntry> rankEntries, RankEntry userRankEntry)
    {
        Aliases = rankEntries.Select(re => re.GetStat("alias") == null ? "Null Alias" : re.GetStat("alias")).ToList();
        Avatars = rankEntries.Select(re => re.GetStat("avatar")).Select(GetAvatar).ToList();
        Ranks = rankEntries.Select(re => re.rank).ToList();
        Scores = rankEntries.Select(re => re.score).ToList();

        CurrentUserIndexInLeaderboard = rankEntries.FindIndex(r => r.rank == userRankEntry.rank);
    }

    /// <summary>
    /// Just a helper that gets a reference to the avatar image that matches the given id. 
    /// </summary>
    private Sprite GetAvatar(string id)
    {
        var accountAvatar = AvatarConfiguration.Instance.Avatars.FirstOrDefault(av => av.Name == id);
        return accountAvatar != null ? accountAvatar.Sprite : AvatarConfiguration.Instance.Default.Sprite;
    }

}
