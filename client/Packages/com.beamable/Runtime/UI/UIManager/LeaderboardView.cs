using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Leaderboard;
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
/// This is an example <see cref="IBeamableView"/>. It is based off the <see cref="LeaderboardsPresenter"/>. For ease of comparison:
/// - 
/// </summary>
public class LeaderboardView : MonoBehaviour, IBeamableView
{
    public interface IViewDependencies : IBeamableView.IDeps
    {
        List<RankEntry> CurrentRankEntries { get; }

        RankEntry CurrentUserRankEntry { get; }

        IUserContext User { get; }

        public bool TestMode { get; set; }

        Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<IViewDependencies> onComplete = null);
    }
    
    
    public BasicBeamableLeaderboardSystemConfig CurrentLeaderboardSystemConfig;
    public BasicBeamableLeaderboardSystemConfig BackUpConfig;

    public GenericButton BackButton;
    public GenericButton TopButton;
    public LeaderboardsRankEntriesPresenter RankEntries;
    public LeaderboardsRankEntryPresenter CurrentUserRankEntry;
    [Space] public UnityEvent BackButtonAction;

    public BeamableView.PlayerCountMode SupportedMode => BeamableView.PlayerCountMode.SinglePlayerUI;

    public async void EnrichWithContext(BeamContext currentContext)
    {
        if (BackButton != null)
        {
            BackButton.onClick.RemoveAllListeners();
            BackButton.onClick.AddListener(() => BackButtonAction.Invoke());
        }

        if (TopButton != null)
        {
            TopButton.onClick.RemoveAllListeners();
            TopButton.onClick.AddListener(() =>
            {
                (CurrentLeaderboardSystemConfig, BackUpConfig) = (BackUpConfig, CurrentLeaderboardSystemConfig);
                EnrichWithContext(currentContext);
            });
        }

        var system = currentContext.ServiceProvider.GetService<IViewDependencies>();

        if (CurrentUserRankEntry != null)
        {
            CurrentUserRankEntry.LoadingIndicator.ToggleLoadingIndicator(true);
            RankEntries.ClearPooledRankedEntries();
        }
        
        var testMode = CurrentLeaderboardSystemConfig.Configuration.TestMode;
        var leaderboardRef = CurrentLeaderboardSystemConfig.Configuration.LeaderboardRef;
        var entriesAmount = CurrentLeaderboardSystemConfig.Configuration.EntriesAmount;
        system.TestMode = testMode;
        Debug.Log($"BEFORE ----- {system.User.UserId} - {system.CurrentUserRankEntry?.score}");
        await system.UpdateState(leaderboardRef, 0, entriesAmount);
        Debug.Log($"AFTER ---- {system.User.UserId} - {system.CurrentUserRankEntry.score}");

        if (CurrentUserRankEntry != null)
        {
            RankEntries.Enrich(system.CurrentRankEntries, system.CurrentUserRankEntry.rank);
            RankEntries.RebuildPooledRankEntries();

            CurrentUserRankEntry.Enrich(system.CurrentUserRankEntry, system.CurrentUserRankEntry.rank);
            CurrentUserRankEntry.RebuildRankEntry();
        }
    }

    public void EnrichWithContext(BeamContext currentContext, int playerIndex)
    {
        throw new NotSupportedException($"This UI does not know how to render multiple players. You can know that by looking at {SupportedMode}.");
    }
    
    
    
}


/// <summary>
/// This is our basic leaderboard system --- It exposes some APIs to fetch data from the backend and rearranges that API with basic information from our backend into a
/// format more easily usable by UI.
/// TODO: The <see cref="LeaderboardView.IViewDependencies"/> needs some work. Ideally, it should not depend on <see cref="RankEntry"/> as that is just the shape of our response.
/// TODO: Most of the time, after we get the response, we can kick some async tasks to go load the avatar sprites for these players and build and prepare the strings.
/// TODO: These primitive and Unity types (or View-specific complex types containing only primitive and Unity types) is what we should declare on any <see cref="IBeamableView.IDeps"/>.
/// TODO: These systems are responsible for TRANSFORMING data we get from the back-end into formats more easily usable by UI/Visual code in Unity.
/// TODO: Doing so should make it easier to reuse the view from other data sources.
/// TODO: I'll setup an example of this as we go.  
/// </summary>
[BeamContextSystem]
public class BeamableDefaultLeaderboardSystem : LeaderboardView.IViewDependencies
{
    [RegisterBeamableDependencies(int.MinValue)]
    public static void Reg(IDependencyBuilder builder)
    {
        builder.RemoveIfExists<LeaderboardView.IViewDependencies>();
        builder.AddSingleton<LeaderboardView.IViewDependencies, BeamableDefaultLeaderboardSystem>();
    }

    /// <summary>
    /// TODO: <see cref="BasicBeamableLeaderboardSystemConfig"/> for an explanation on why I think this is wrong/bad/evil and was a bad idea I had in the first place.
    /// TODO: The properties below, though, are fine. It's just this declaration living here and being used in the <see cref="BasicBeamableLeaderboardSystemConfig"/> ScriptableObject that is bad.
    /// </summary>
    [Serializable]
    public struct SerializableConfig
    {
        public LeaderboardRef LeaderboardRef;
        public int EntriesAmount;
        [Header("Debug")] public bool TestMode;
    }

    
    /// <summary>
    /// Whether or not this is using mocked test data.
    /// </summary>
    public bool TestMode { get; set; }
    
    /// <summary>
    /// Current list of rank entries.
    /// TODO: This is fine. But it really should be what is fulfilling the <see cref="LeaderboardView.IViewDependencies"/> interface. See comments on this class on why I think that is the case.
    /// </summary>
    public List<RankEntry> CurrentRankEntries { get; private set; } = new List<RankEntry>();
    
    /// <summary>
    /// Current rank for the player that is authenticated with this BeamContext.
    /// TODO: This is fine. But it really should be what is fulfilling the <see cref="LeaderboardView.IViewDependencies"/> interface. See comments on this class on why I think that is the case.
    /// </summary>
    public RankEntry CurrentUserRankEntry { get; private set; }
    
    /// <summary>
    /// Just an easy way to access the user that owns this BeamContext.
    /// TODO: This is fine as a property here. BUT, we should remove this from <see cref="LeaderboardView.IViewDependencies"/>. See comments on this class on why I think that is the case.
    /// </summary>
    public IUserContext User { get; }

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
        User = ctx;
    }

    /// <summary>
    /// Just a simple API that will go talk to the Back-End, wait for the response and update this system's internal state.
    /// TODO: It should also update whatever it needs to update to fulfill any <see cref="IBeamableView.IDeps"/> it implements.
    /// TODO: I'll be setting up some examples as I continue exploring this architecture.
    /// </summary>
    public virtual async Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<LeaderboardView.IViewDependencies> onComplete = null)
    {
        if (TestMode)
        {
            await Task.Delay(1000);
            CurrentUserRankEntry = LeaderboardsModelHelper.GenerateCurrentUserRankEntryTestData("_aliasStatObject.StatKey", "100");
            CurrentRankEntries = LeaderboardsModelHelper.GenerateLeaderboardsTestData(firstEntryId, firstEntryId + entriesAmount, CurrentUserRankEntry, "_aliasStatObject.StatKey", "100");
        }
        else
        {
            try
            {
                await _leaderboardService.GetUser(_leaderboardRef, User.UserId).Then(re => CurrentUserRankEntry = re);
                await _leaderboardService.GetBoard(_leaderboardRef, firstEntryId, firstEntryId + entriesAmount).Then(ld => CurrentRankEntries = ld.ToList());
            }
            catch
            {
                CurrentUserRankEntry = new RankEntry() { columns = new RankEntryColumns() { score = -1 } };
                CurrentRankEntries = Enumerable.Repeat(new RankEntry() { columns = new RankEntryColumns() { score = -1 } }, 10).ToList();
            }

            onComplete?.Invoke(this);
        }
    }
}


 

/// <summary>
/// THIS IS HOW USERS WOULD OVERRIDE OUR BEHAVIOUR THAT FEEDS THE UI IT'S. THEY CAN:
///  - Inherit from our base implementation and add behaviour makes thing slightly different.
///  TODO: - Completely replace the source of data (go fetch from a C#MS they implement that is game-specific).
///  TODO: - Add data to the View coming from some other client system they have.
///  TODO: - Add data to the View coming from some C#MS that holds additional data tied to each leaderboard.
///
/// I'm fairly confident in saying that none of the above would be "hard to do". And ALL could be done in less than 1 day's worth of work (depending on size of C#MS or data source).
/// But I believe only extremely complex user overrides would take longer than a week to do [full on re-implementation of back-end systems with C#MS]. 90% of the cases I can think of can easily
/// be accomplished through this pattern and the other 10% have the work mostly on the system implementation details WHICH IS WHAT I WOULD EXPECT --- the cost of your system needs to scale with it's
/// implementation complexity and, hopefully, almost nothing else. 
/// </summary>
[BeamContextSystem]
public class UserOverrideLeaderboardSystem : BeamableDefaultLeaderboardSystem
{
    [RegisterBeamableDependencies()]
    public static void RegUserSystem(IDependencyBuilder builder)
    {
        builder.RemoveIfExists<LeaderboardView.IViewDependencies>();
        builder.AddSingleton<LeaderboardView.IViewDependencies, UserOverrideLeaderboardSystem>();
    }

    public UserOverrideLeaderboardSystem(LeaderboardService leaderboardService, IUserContext ctx) : base(leaderboardService, ctx) { }

    public override async Promise UpdateState(LeaderboardRef _leaderboardRef, int firstEntryId, int entriesAmount, Action<LeaderboardView.IViewDependencies> onComplete = null)
    {
        Debug.Log("I'M A PIECE USER CODE THAT HAS HIJACKED YOUR PREFAB MUAHAHAHAHAHAHA!");
        await base.UpdateState(_leaderboardRef, firstEntryId, entriesAmount, onComplete);
    }
}
