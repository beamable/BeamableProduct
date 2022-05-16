using Beamable.Server;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
    /// <summary>
    /// This <see cref="Microservice"/> handles all processes involved in starting match with a League of Legends Solo Queue-style draft system.
    /// <para/>
    /// It uses the <see cref="MatchPreparationStorage"/> <see cref="StorageObject"/> as well as the <see cref="MatchAcceptance"/> and <see cref="MatchDraft"/>
    /// <see cref="StorageDocument"/>s to keep track of the state of a given draft.
    /// <para/>
    /// It also exposes a <see cref="OnMatchMade"/> function that is meant to be called as a blocking webhook whenever a match is made, prior to notifying players that it was made.
    /// Since this feature isn't ready, this sample fakes this call using a <see cref="BeamContext"/>.
    /// <para/>
    /// When using <see cref="StorageObject"/>s, it is good practice to explicitly define and guarantee the existence of database indexes so we can efficiently query the data stored in it.
    /// We recommend this be done when the Microservice is being initialized. To hook into a Microservice's initialization, declare a "static Task" function that takes in a <see cref="IServiceInitializer"/>
    /// and mark it with the <see cref="InitializeServicesAttribute"/>. You can use this service initializer to setup your database indexes.
    /// </summary>
    [Microservice("MatchPreparationService")]
    public partial class MatchPreparationService : Microservice
    {
        /// <summary>
        /// "Auto-magically" called by Microservice initialization flow. Ensures we have the indexes for our <see cref="MatchPreparationStorage"/> correctly setup. 
        /// </summary>
        /// <param name="serviceInitializer"></param>
        [InitializeServices]
        public static async Task InitializeStorageIndex(IServiceInitializer serviceInitializer)
        {
            var storage = serviceInitializer.GetService<IStorageObjectConnectionProvider>();
            var matchAcceptanceCollection = await storage.GetCollection<MatchPreparationStorage, MatchAcceptance>();

            // Create index for the preparation collection.
            var matchAcceptanceIndex = Builders<MatchAcceptance>.IndexKeys.Hashed(matchAcceptance => matchAcceptance.MatchId);
            await matchAcceptanceCollection.Indexes.CreateOneAsync(new CreateIndexModel<MatchAcceptance>(matchAcceptanceIndex));

            // Create index for the draft collection.
            var matchDraftCollection = await storage.GetCollection<MatchPreparationStorage, MatchDraft>();
            var matchDraftIndex = Builders<MatchDraft>.IndexKeys.Hashed(draft => draft.MatchId);
            await matchDraftCollection.Indexes.CreateOneAsync(new CreateIndexModel<MatchDraft>(matchDraftIndex));
        }

        /// <summary>
        /// Callable by a client trying to accept the match with the given <paramref name="matchId"/>.
        /// </summary>
        /// <returns>The state of <see cref="MatchAcceptance"/> for the entire match (contains information about other player's accepting state).</returns>
        [ClientCallable]
        public async Task<MatchAcceptance> AcceptMatch(string matchId)
        {
            // Get the id for the player that is making the request
            var requesterId = Context.UserId;

            // Find the match this player is in.
            var matchAcceptanceStorage = await Storage.GetCollection<MatchPreparationStorage, MatchAcceptance>();
            var match = await matchAcceptanceStorage.FindAsync(matchAcceptance => matchAcceptance.MatchId == matchId && matchAcceptance.ExpectedPlayers.Contains(requesterId));
            var acceptance = match.FirstOrDefault();

            // If null, return a not found match so the client can handle this.
            if (acceptance == null)
                return new MatchAcceptance() { CurrentState = MatchAcceptanceState.NotFound };

            // If the person trying to accept isn't in the match
            var requesterIdx = new List<long>(acceptance.ExpectedPlayers).IndexOf(requesterId);
            if (requesterIdx == -1)
                return new MatchAcceptance() { CurrentState = MatchAcceptanceState.NotFound };

            // Ignore requests to accept by the same player.
            if (acceptance.AcceptedPlayers[requesterIdx])
                return acceptance;

            // Set the accepted player to true.
            acceptance.AcceptedPlayers[requesterIdx] = true;

            // Otherwise, update player count and check if the match is now ready
            acceptance.AcceptedPlayerCount += 1;
            if (acceptance.CurrentState == MatchAcceptanceState.Accepting && acceptance.AcceptedPlayerCount >= acceptance.ExpectedPlayers.Length)
                acceptance.CurrentState = MatchAcceptanceState.Ready;

            // Replace the acceptance with the updated one.
            _ = await matchAcceptanceStorage
                .FindOneAndReplaceAsync(acc => acc.MatchId == matchId && acc.ExpectedPlayers.Contains(requesterId), acceptance);

            // If all players are ready, create a MatchDraft document for this MatchId and store it in the MatchPreparationStorage.
            // In the future, we will also be able to notify every player in the match that the match is ready directly so that they don't need to poll against these collections.
            if (acceptance.CurrentState == MatchAcceptanceState.Ready)
            {
                var matchDraftStorage = await Storage.GetCollection<MatchPreparationStorage, MatchDraft>();
                var draftTypeRef = (DraftSimType)await Services.Content.GetContent(acceptance.GameTypeId, typeof(DraftSimType));

                await matchDraftStorage.InsertOneAsync(new MatchDraft()
                {
                    MatchId = acceptance.MatchId,
                    GameTypeId = acceptance.GameTypeId,
                    CurrentPickIdx = 0,
                    TeamAPlayerIds = acceptance.ExpectedPlayers.Take(draftTypeRef.TeamSize).ToList(),
                    TeamBPlayerIds = acceptance.ExpectedPlayers.Skip(draftTypeRef.TeamSize).Take(draftTypeRef.TeamSize).ToList(),

                    TeamABannedCharacters = Enumerable.Repeat(-1, draftTypeRef.TeamSize).ToArray(),
                    TeamBBannedCharacters = Enumerable.Repeat(-1, draftTypeRef.TeamSize).ToArray(),

                    TeamALockedInCharacters = Enumerable.Repeat(-1, draftTypeRef.TeamSize).ToArray(),
                    TeamBLockedInCharacters = Enumerable.Repeat(-1, draftTypeRef.TeamSize).ToArray(),
                });

                // TODO: We would need a way to notify all players in this match the the final player is ready
                // Ideally, we would do this via an authenticated WebSocket connection or the exposed PubNub stack.  
                await Services.Notifications.NotifyPlayer(acceptance.ExpectedPlayers.ToList(), MatchAcceptance.NOTIFICATION_MATCH_READY, acceptance);

                // For now, we just have a GetCurrentMatchAcceptanceState(string matchId) declared here that the client is polls against to know the match was ready.
            }

            return acceptance;
        }

        /// <summary>
        /// Callable by a client trying to decline the match with the given <paramref name="matchId"/>.
        /// </summary>
        /// <returns>The state of <see cref="MatchAcceptance"/> for the entire match (contains information about other player's accepting state).</returns>
        [ClientCallable]
        public async Task<MatchAcceptance> DeclineMatch(string matchId)
        {
            // Get the id for the player that is making the request
            var requesterId = Context.UserId;

            // TODO: If whoever downloads this sample wishes to experiment, try adding some guards here against players outside of the match attempting to decline it.

            // Prepare the update command. 
            var updateCmd = Builders<MatchAcceptance>.Update
                .Set(acc => acc.CurrentState, MatchAcceptanceState.PlayerDeclined);

            // Replace the acceptance with the updated one.
            var matchAcceptanceStorage = await Storage.GetCollection<MatchPreparationStorage, MatchAcceptance>();
            var updatedAcceptance = await matchAcceptanceStorage
                .FindOneAndUpdateAsync<MatchAcceptance>(acc => acc.MatchId == matchId && acc.ExpectedPlayers.Contains(requesterId),
                    updateCmd,
                    new FindOneAndUpdateOptions<MatchAcceptance>() { IsUpsert = true, ReturnDocument = ReturnDocument.After });

            // In the future, we will notify every player in the match directly that the match was declined so that they don't need to poll against these collections.
            await Services.Notifications.NotifyPlayer(updatedAcceptance.ExpectedPlayers.ToList(),MatchAcceptance.NOTIFICATION_MATCH_DECLINED, updatedAcceptance);
            
            return updatedAcceptance;
        }

        /// <summary>
        /// Calling this from an OnMatchWebhook (that is blocking and not fire-and-forget) would guarantee that the document exists by the time the client gets the notification about
        /// the match).
        /// While we don't provide this webhook, at the moment -- this is called using a "fake" BeamContext pretending to be our Matchmaking service.
        /// </summary>
        [ClientCallable]
        public async Task OnMatchMade(string matchId, string gameTypeId, long[] playerIds)
        {
            var gameTypeData = (DraftSimType)await Services.Content.GetContent(gameTypeId, typeof(DraftSimType));

            // Should actually be a custom field called TimeToAcceptMatch.
            var timeToAcceptMatch = gameTypeData.maxWaitDurationSecs.Value;

            var matchAcceptanceCollection = await Storage.GetCollection<MatchPreparationStorage, MatchAcceptance>();
            await matchAcceptanceCollection.InsertOneAsync(new MatchAcceptance()
            {
                MatchId = matchId,
                GameTypeId = gameTypeId,

                ExpectedPlayers = playerIds,
                // This should really be a bit field...
                AcceptedPlayers = Enumerable.Repeat(false, playerIds.Length).ToArray(),
                AcceptedPlayerCount = 0,
                MatchEndTimeStamp = DateTime.Now.Ticks + timeToAcceptMatch
            });
        }
    }
}
