using Beamable.Common.Api;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Dependencies;
using Beamable.Experimental.Api.Matchmaking;
using Beamable.Server.Clients;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
    /// <summary>
    /// System that controls a League of Legends Solo Queue-style draft system.
    /// This version is initialized with every <see cref="Beamable.BeamContext"/> via Beamable's Dependency Injection (DI) framework.
    /// For a version of this same system meant to be explicitly initialized and managed, see <see cref="MatchStartSystemV1"/>.
    ///
    /// <para/>
    /// To add register a system with Beamable's DI-framework, add a <see cref="BeamContextSystemAttribute"/> to a class containing a static function with
    /// <see cref="RegisterBeamableDependenciesAttribute"/>. This function must take in a <see cref="IDependencyBuilder"/> which you can use to register systems to be available
    /// via the <see cref="BeamContext.ServiceProvider"/>.
    /// </summary>
    [BeamContextSystem]
    public class MobaDraftPlayerSystem
    {
        /// <summary>
        /// Add this system as a scoped dependency of every <see cref="BeamContext"/>.
        /// </summary>
        [RegisterBeamableDependencies()]
        public static void RegisterService(IDependencyBuilder builder) => builder.AddSingleton<MobaDraftPlayerSystem>();

        /// <summary>
        /// Fetches the authenticated user id of the user owning this <see cref="BeamContext"/>.
        /// </summary>
        public long AuthPlayerId => _authenticatedUserContext.UserId;
        
        /// <summary>
        /// Injected referenced to the <see cref="MatchPreparationServiceClient"/> Microservice.
        /// </summary>
        private readonly MatchPreparationServiceClient _matchPreparationBackend;

        /// <summary>
        /// Injected reference to the <see cref="IContentApi"/> service that lives in the <see cref="BeamContext"/>.
        /// </summary>
        private readonly IContentApi _contentApi;
        
        /// <summary>
        /// Injected reference to the <see cref="IUserContext"/> object representing an authenticated user.
        /// </summary>
        private readonly IUserContext _authenticatedUserContext;
        
        /// <summary>
        /// Injected reference to the <see cref="INotificationService"/> object that we use to receive notifications from services.
        /// </summary>
        private readonly INotificationService _notificationService;

        
        /// <summary>
        /// <see cref="Match.matchId"/> for the match the authenticated user of this context is participating in.
        /// </summary>
        public string MatchId { get; private set; }
        
        /// <summary>
        /// The current <see cref="DraftStates"/> of the authenticated user of this context.
        /// </summary>
        public DraftStates CurrentDraftState { get; private set; }
        
        /// <summary>
        /// A resolved <see cref="DraftSimType"/> containing the rules of this draft.
        /// </summary>
        public DraftSimType Rules { get; private set; }

        /// <summary>
        /// A local copy of this draft's <see cref="MatchAcceptance"/>. This is kept in sync via <see cref="PollMatchAcceptance"/>, <see cref="_lastPoll"/> and <see cref="_pollFreq"/>.
        /// </summary>
        public MatchAcceptance AcceptanceState { get; private set; }
        
        
        /// <summary>
        /// A local copy of this draft's <see cref="MatchDraft"/>. This is kept in sync via <see cref="PollDraft"/>, <see cref="_lastPoll"/> and <see cref="_pollFreq"/>.
        /// </summary>
        public MatchDraft DraftState { get; private set; }
        
        
        /// <summary>
        /// A list of all characters that are selectable in this draft by default (pre-bans).
        /// </summary>
        public IReadOnlyList<int> SelectableCharIds { get; private set; }

        /// <summary>
        /// Constructor used by the DI-framework to know which other systems this system depends on. These parameters get "auto-magically" injected via Beamable's DI framework.
        /// </summary>
        public MobaDraftPlayerSystem(IUserContext authenticatedUserContext, 
                                     MatchPreparationServiceClient matchPreparationMicroservice,
                                     IContentApi contentService,
                                     INotificationService notificationService)
        {
            _authenticatedUserContext = authenticatedUserContext;
            _matchPreparationBackend = matchPreparationMicroservice;
            _contentApi = contentService;
            _notificationService = notificationService;
        }
        
        /// <summary>
        /// Starts a Match Start Process for the given <paramref name="matchId"/> with the player represented by <see cref="AuthPlayer"/>.
        /// In this case, we also take in a <see cref="DraftSimType"/>'s content id and the pool of characters we are supposed to draft from.
        /// </summary>
        public void StartMatchProcessKickOff(string matchId, string draftRulesContentId, List<int> selectableCharacters)
        {
            // Uses the injected IContentApi to get the draft rules and then initializes the current match state. 
            _contentApi.GetContent(draftRulesContentId, typeof(DraftSimType)).Then(draftSimType =>
            {
                Rules = (DraftSimType)draftSimType;
                
                MatchId = matchId;
                AcceptanceState = new MatchAcceptance() { MatchId = matchId, AcceptedPlayers = new bool[Rules.TotalNumberOfDraftPicks], CurrentState = MatchAcceptanceState.Accepting, AcceptedPlayerCount = -1, };
                DraftState = new MatchDraft() { MatchId = matchId, CurrentPickIdx = -1 };

                CurrentDraftState = DraftStates.AcceptDecline;

                SelectableCharIds = selectableCharacters;
            });
            
            _notificationService.Subscribe<MatchAcceptance>(MatchAcceptance.NOTIFICATION_MATCH_READY, OnMatchReadyToStart);
            _notificationService.Subscribe<MatchAcceptance>(MatchAcceptance.NOTIFICATION_MATCH_DECLINED, OnMatchDeclined);
        }

        private void OnMatchReadyToStart(MatchAcceptance readyAcceptance)
        {
	        if (AcceptanceState.ExpectedPlayers == null)
		        AcceptanceState = readyAcceptance;
	        else
		        MergeAcceptanceStates(AcceptanceState, readyAcceptance);

	        // Parses the acceptance state and modify the CurrentDraftState for this authenticated user.
	        switch (AcceptanceState.CurrentState)
	        {
		        case MatchAcceptanceState.PlayerDeclined:
			        CurrentDraftState = DraftStates.Declined;
			        break;
		        case MatchAcceptanceState.Ready:
		        {
			        CurrentDraftState = DraftStates.Banning;
			        
			        _notificationService.Subscribe<MatchDraft>(MatchDraft.NOTIFICATION_CHARACTER_BANNED, OnCharacterBanned);
			        _notificationService.Subscribe<MatchDraft>(MatchDraft.NOTIFICATION_CHARACTER_SELECTED, OnCharacterSelected);
			        
			        break;
		        }
		        default:
			        CurrentDraftState = CurrentDraftState;
			        break;
	        }
        }

        private void OnMatchDeclined(MatchAcceptance declinedAcceptance)
        {
	        if (AcceptanceState.ExpectedPlayers == null)
		        AcceptanceState = declinedAcceptance;
	        else
				MergeAcceptanceStates(AcceptanceState, declinedAcceptance);
	        
	        // Parses the acceptance state and modify the CurrentDraftState for this authenticated user.
	        switch (AcceptanceState.CurrentState)
	        {
		        case MatchAcceptanceState.PlayerDeclined:
			        CurrentDraftState = DraftStates.Declined;
			        break;
		        case MatchAcceptanceState.Ready:
			        CurrentDraftState = DraftStates.Banning;
			        break;
		        default:
			        CurrentDraftState = CurrentDraftState;
			        break;
	        }
        }
        
        
        private void OnCharacterSelected(MatchDraft updatedDraft)
        {
	        MergeDraftStates(DraftState, updatedDraft);
	        UpdateDraftStateDuringLockIn();
        }
        
        private void OnCharacterBanned(MatchDraft updatedDraft)
        {
	        if (DraftState.TeamAPlayerIds == null || DraftState.TeamBPlayerIds == null)
		        DraftState = updatedDraft;
	        else
				MergeDraftStates(DraftState, updatedDraft);

	        if (GetMissingBansPlayerIds().Count == 0)
	        {
		        UpdateDraftStateDuringLockIn();
	        }
        }

        private void UpdateDraftStateDuringLockIn()
        {
	        DraftState.GetPlayerAndTeamIndices(_authenticatedUserContext.UserId, out var playerIdx, out var teamIdx);

	        var currentTeamIdx = Rules.PerPickIdxTeams[DraftState.CurrentPickIdx];
	        var currentTeamPlayerIdx = Rules.PerPickIdxPlayerIdx[DraftState.CurrentPickIdx];

	        // If the rules say it's my turn, I'm picking my character.
	        if (currentTeamIdx == teamIdx && currentTeamPlayerIdx == playerIdx)
	        {
		        CurrentDraftState = DraftStates.Picking;
	        }
	        else
	        {
		        // If the player picking now is before me, I'm waiting for my turn to pick.
		        if (currentTeamPlayerIdx < playerIdx)
			        CurrentDraftState = DraftStates.WaitingTurnToPick;
		        // Otherwise, I'm waiting for the draft to end, if all the picks haven't finished yet.
		        else
			        CurrentDraftState = DraftState.CurrentPickIdx >= Rules.TotalNumberOfDraftPicks ? DraftStates.Ready : DraftStates.WaitingDraftEnd;
	        }
        }

        private static void MergeAcceptanceStates(MatchAcceptance local, MatchAcceptance remote)
        {
	        local.CurrentState = local.CurrentState == MatchAcceptanceState.Ready || local.CurrentState == MatchAcceptanceState.PlayerDeclined ? local.CurrentState : remote.CurrentState;

	        for (var i = 0; i < local.AcceptedPlayers.Length; i++) local.AcceptedPlayers[i] |= remote.AcceptedPlayers[i];

	        local.AcceptedPlayerCount = local.AcceptedPlayerCount > remote.AcceptedPlayerCount ? local.AcceptedPlayerCount : remote.AcceptedPlayerCount;
        }
        
        private static void MergeDraftStates(MatchDraft localDraftState, MatchDraft remoteDraftState)
        {
	        localDraftState.CurrentPickIdx = remoteDraftState.CurrentPickIdx;

	        for (int i = 0; i < localDraftState.TeamABannedCharacters.Length; i++)
		        localDraftState.TeamABannedCharacters[i] = localDraftState.TeamABannedCharacters[i] == -1 ? remoteDraftState.TeamABannedCharacters[i] : localDraftState.TeamABannedCharacters[i];

	        for (int i = 0; i < localDraftState.TeamBBannedCharacters.Length; i++)
		        localDraftState.TeamBBannedCharacters[i] = localDraftState.TeamBBannedCharacters[i] == -1 ? remoteDraftState.TeamBBannedCharacters[i] : localDraftState.TeamBBannedCharacters[i];

	        for (int i = 0; i < localDraftState.TeamALockedInCharacters.Length; i++)
		        localDraftState.TeamALockedInCharacters[i] = localDraftState.TeamALockedInCharacters[i] == -1 ? remoteDraftState.TeamALockedInCharacters[i] : localDraftState.TeamALockedInCharacters[i];

	        for (int i = 0; i < localDraftState.TeamBLockedInCharacters.Length; i++)
		        localDraftState.TeamBLockedInCharacters[i] = localDraftState.TeamBLockedInCharacters[i] == -1 ? remoteDraftState.TeamBLockedInCharacters[i] : localDraftState.TeamBLockedInCharacters[i];
        }


        /// <summary>
        /// Invalidates a Match start. Basically, will stop polling for updates on <see cref="MatchAcceptance"/> and <see cref="MatchDraft"/>.
        /// </summary>
        public void InvalidateMatchStart()
        {
            MatchId = "";
            AcceptanceState = null;
            DraftState = null;
            CurrentDraftState = DraftStates.NotReady;
            
            _notificationService.Unsubscribe<MatchAcceptance>(MatchAcceptance.NOTIFICATION_MATCH_READY, OnMatchReadyToStart);
            _notificationService.Unsubscribe<MatchAcceptance>(MatchAcceptance.NOTIFICATION_MATCH_DECLINED, OnMatchDeclined);
            
            _notificationService.Unsubscribe<MatchDraft>(MatchDraft.NOTIFICATION_CHARACTER_BANNED, OnCharacterBanned);
            _notificationService.Unsubscribe<MatchDraft>(MatchDraft.NOTIFICATION_CHARACTER_SELECTED, OnCharacterSelected);
        }

        /// <summary>
        /// Assert that no one is misusing the system by calling things out of order.
        /// </summary>
        private void CheckMatchStartKickedOff() =>
            Assert.IsTrue(!string.IsNullOrEmpty(MatchId), "No Match Start Process has started. Ensure you have called StartMatchProcessKickoff before you call this.");
        
        
        /// <summary>
        /// Can be called to try and accept the current running match.
        /// </summary>
        public async void AcceptMatch()
        {
            CheckMatchStartKickedOff();
            
            var acceptance = await _matchPreparationBackend.AcceptMatch(MatchId);
           
            if (AcceptanceState.ExpectedPlayers == null)
	            AcceptanceState = acceptance;
            else
	            MergeAcceptanceStates(AcceptanceState, acceptance);
            
            CurrentDraftState = CurrentDraftState == DraftStates.Ready || CurrentDraftState == DraftStates.Declined ? CurrentDraftState : DraftStates.WaitingForAcceptance;
        }

        /// <summary>
        /// Can be called to decline a match. Will cause the match's <see cref="MatchAcceptance.CurrentState"/> to change into <see cref="MatchAcceptanceState.PlayerDeclined"/>.
        /// This will propagate out to all other players as they <see cref="PollMatchAcceptance"/>.
        /// </summary>
        public async void DeclineMatch()
        {
            CheckMatchStartKickedOff();
            
            var acceptance = await _matchPreparationBackend.DeclineMatch(MatchId);
            
            if (AcceptanceState.ExpectedPlayers == null)
	            AcceptanceState = acceptance;
            else
				MergeAcceptanceStates(AcceptanceState, acceptance);
            
            CurrentDraftState = DraftStates.Declined;
        }

        /// <summary>
        /// Call to ban a character for the authenticated user of this system in the current match. 
        /// </summary>
        public async void BanCharacter(int characterId)
        {
            CheckMatchStartKickedOff();
            
            var updatedDraft = await _matchPreparationBackend.BanCharacter(MatchId, characterId);
            if (DraftState.TeamAPlayerIds == null || DraftState.TeamBPlayerIds == null)
	            DraftState = updatedDraft;
            else
	            MergeDraftStates(DraftState, updatedDraft);
            
            if (GetMissingBansPlayerIds().Count == 0)
            {
	            UpdateDraftStateDuringLockIn();
            }
            else
            {
	            CurrentDraftState = DraftStates.WaitingBanEnd;
            }
        }

        /// <summary>
        /// Call to lock in a character for the authenticated user of this system in the current match. 
        /// </summary>
        public async void LockInCharacter(int charId)
        {
            CheckMatchStartKickedOff();
            
            var updatedDraft = await _matchPreparationBackend.SelectCharacter(MatchId, charId);
            if (DraftState.TeamAPlayerIds == null || DraftState.TeamBPlayerIds == null)
	            DraftState = updatedDraft;
            else
	            MergeDraftStates(DraftState, updatedDraft);
            
            CurrentDraftState = DraftState.CurrentPickIdx >= Rules.TotalNumberOfDraftPicks ? DraftStates.Ready : DraftStates.WaitingDraftEnd;
        }

        /// <summary>
        /// Helper function to generate a list of available picks. Excludes locked in characters and banned characters from the list of possible picks.
        /// </summary>
        public List<int> GetPossiblePicks() => SelectableCharIds
            .Except(
                DraftState.TeamABannedCharacters.Union(DraftState.TeamBBannedCharacters).Union(DraftState.TeamALockedInCharacters).Union(DraftState.TeamBLockedInCharacters)
            ).ToList();

        /// <summary>
        /// Helper function to generate a list of all player ids that have yet to ban a character.
        /// </summary>
        public List<long> GetMissingBansPlayerIds()
        {
            var missingPlayerBans = new List<long>();

            for (var i = 0; i < Rules.TeamSize; i++)
            {
                var hasTeamAPicked = DraftState.TeamABannedCharacters[i] != -1;
                if (!hasTeamAPicked) missingPlayerBans.Add(DraftState.TeamAPlayerIds[i]);
            }

            for (var i = 0; i < Rules.TeamSize; i++)
            {
                var hasTeamBPicked = DraftState.TeamBBannedCharacters[i] != -1;
                if (!hasTeamBPicked) missingPlayerBans.Add(DraftState.TeamAPlayerIds[i]);
            }

            return missingPlayerBans;
        }
    }
}
