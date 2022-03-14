using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Api.Leaderboard;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Common.Leaderboards;
using UnityEngine;

/// <summary>
/// THIS IS HOW USERS WOULD OVERRIDE OUR BEHAVIOUR THAT FEEDS THE UI IT'S. THEY CAN:
///  - Inherit from our base implementation and add behaviour makes thing slightly different.
/// </summary>
// [BeamContextSystem]
// public class UserOverrideLeaderboardSystem : BeamableDefaultLeaderboardSystem
// {
//     [RegisterBeamableDependencies()]
//     public static void RegUserSystem(IDependencyBuilder builder)
//     {
//         builder.RemoveIfExists<LeaderboardView.ILeaderboardDeps>();
//         builder.AddSingleton<LeaderboardView.ILeaderboardDeps, UserOverrideLeaderboardSystem>();
//     }
//
//     public UserOverrideLeaderboardSystem(LeaderboardService leaderboardService, IUserContext ctx) : base(leaderboardService, ctx) { }
//
//     public override async Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<LeaderboardView.ILeaderboardDeps> onComplete = null)
//     {
//         Debug.Log("I'M A PIECE USER CODE THAT HAS HIJACKED YOUR PREFAB MUAHAHAHAHAHAHA!");
//         await base.UpdateState(_leaderboardRef, firstEntryId, entriesAmount, onComplete);
//     }
// }


/// <summary>
/// This is an example of how a user can:
///    - Derive from our systems to add functionality to it (<see cref="UpdateState"/>)
///    - Make it compatible with <see cref="IBeamableView.IDeps"/> that it originally wasn't.    
/// </summary>
// [BeamContextSystem]
// public class UserProfileHackySystem : BeamableDefaultLeaderboardSystem, UserProfileView.IHeaderDeps
// {
//     [RegisterBeamableDependencies()]
//     public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
//     {
//         builder.SetupUnderlyingSystemSingleton<UserProfileHackySystem, // Actual concrete system implementation that satisfies all of the UI dependencies. 
//             UserProfileView.IHeaderDeps, // UI Dependencies for UserProfileView's header 
//             UserProfileView.IRankDeps, // UI Dependencies for UserProfileView's Rank data
//             LeaderboardView.ILeaderboardDeps>(); // UI Dependencies for LeaderboardView's data
//     }
//
//     public UserProfileHackySystem(LeaderboardService leaderboardService, IUserContext ctx) : base(leaderboardService, ctx)
//     {
//     }
//
//     public override async Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<LeaderboardView.ILeaderboardDeps> onComplete = null)
//     {
//         await base.UpdateState(_leaderboardRef, firstEntryId, entriesAmount, onComplete);
//         Aliases = Aliases.Select(a => $"{a} - Potato").ToList();
//     }
//
//     public string GetUserAlias() => CurrentUserIndexInLeaderboard < 0 ? "No Player" : Aliases[CurrentUserIndexInLeaderboard];
//
//     public Sprite GetUserAvatar() => CurrentUserIndexInLeaderboard < 0 ? null : Avatars[CurrentUserIndexInLeaderboard];
// }

/// <summary>
/// This is an example of how a user can:
///    - Use our DI framework to modify our system's and how they communicate with our views via composition (without inheritance).
///    - Make it compatible with <see cref="IBeamableViewDeps"/> that it originally wasn't.
/// </summary>
[BeamContextSystem]
public class UserProfileHackySystem2 : UserProfileView.IHeaderDeps, UserProfileView.IRankDeps, LeaderboardView.ILeaderboardDeps
{
    [RegisterBeamableDependencies()]
    public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
    {
        builder.SetupUnderlyingSystemSingleton<UserProfileHackySystem2, // Actual concrete system implementation that satisfies all of the UI dependencies. 
            UserProfileView.IHeaderDeps, // UI Dependencies for UserProfileView's header 
            UserProfileView.IRankDeps, // UI Dependencies for UserProfileView's Rank data
            LeaderboardView.ILeaderboardDeps>(); // UI Dependencies for LeaderboardView's data
    }

    private readonly BeamableDefaultLeaderboardSystem _leaderboardSystem;
    private readonly IUserContext _loggedUser;

    public UserProfileHackySystem2(BeamableDefaultLeaderboardSystem system, IUserContext ctx)
    {
        _leaderboardSystem = system;
        _loggedUser = ctx;
    }

    public bool TestMode
    {
        get => _leaderboardSystem.TestMode;
        set => _leaderboardSystem.TestMode = value;
    }

    public IReadOnlyList<string> Aliases
    {
        get => _leaderboardSystem.Aliases;
        set => _leaderboardSystem.Aliases = value;
    }

    public IReadOnlyList<Sprite> Avatars => _leaderboardSystem.Avatars;
    public IReadOnlyList<long> Ranks => _leaderboardSystem.Ranks;
    public IReadOnlyList<double> Scores => _leaderboardSystem.Scores;

    public int CurrentUserIndexInLeaderboard => _leaderboardSystem.CurrentUserIndexInLeaderboard;


    public async Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<LeaderboardView.ILeaderboardDeps> onComplete = null)
    {
        await _leaderboardSystem.UpdateState(_leaderboardRef, firstEntryId, entriesAmount, onComplete);
        Aliases = Aliases.Select(a => $"{a} - {_loggedUser.UserId}").ToList();
    }

    public string GetUserAlias() => CurrentUserIndexInLeaderboard < 0 ? "No Player" : Aliases[CurrentUserIndexInLeaderboard];
    public Sprite GetUserAvatar() => CurrentUserIndexInLeaderboard < 0 ? null : Avatars[CurrentUserIndexInLeaderboard];
    public long GetUserRank() => CurrentUserIndexInLeaderboard < 0 ? -1 : _leaderboardSystem.Ranks[CurrentUserIndexInLeaderboard];
}
