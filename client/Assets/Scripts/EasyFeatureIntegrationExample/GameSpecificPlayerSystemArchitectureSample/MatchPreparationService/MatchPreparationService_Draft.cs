using Beamable.Server;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
    public partial class MatchPreparationService
    {
        /// <summary>
        /// Called to ban a character with the given <paramref name="characterId"/> in the given <paramref name="matchId"/>. 
        /// </summary>
        /// <returns>The updated <see cref="MatchDraft"/> state.</returns>
        [ClientCallable]
        public async Task<MatchDraft> BanCharacter(string matchId, int characterId)
        {
            // Get the id for the player that is making the request
            var requesterId = Context.UserId;

            // Find the match this player is in.
            var draftStorage = await Storage.GetCollection<MatchPreparationStorage, MatchDraft>();
            var queryResults = await draftStorage.FindAsync(matchDraft => matchDraft.MatchId == matchId &&
                                                                     (matchDraft.TeamAPlayerIds.Contains(requesterId) ||
                                                                      matchDraft.TeamBPlayerIds.Contains(requesterId)));

            var draft = queryResults.FirstOrDefault();
            if (draft == null)
                return new MatchDraft() { MatchId = "NOT_FOUND", CurrentPickIdx = -1 };

            // TODO: If whoever downloads this sample wishes to experiment, try adding some guards here against players outside of the match attempting to ban characters.

            // Get the game type ref TODO: Normally, this would contain a definition of the draft order
            var draftTypeRef = (DraftSimType)await Services.Content.GetContent(draft.GameTypeId, typeof(DraftSimType));

            // If the draft is already finished, just return it.
            var totalNumberOfDraftPicks = draftTypeRef.TotalNumberOfDraftPicks;
            if (totalNumberOfDraftPicks == draft.CurrentPickIdx)
                return draft;

            // Get the idx into the team array for the requesting player and the team of the requesting player.
            draft.GetPlayerAndTeamIndices(requesterId, out var requesterIdx, out var requesterTeam);

            // If the player is not in the match draft --- just return the draft without updating it.
            if (requesterTeam == -1 || requesterIdx == -1)
                return draft;

            // Set the character into the banned slot and return the draft.
            if (requesterTeam == 0)
                draft.TeamABannedCharacters[requesterIdx] = characterId;
            else
                draft.TeamBBannedCharacters[requesterIdx] = characterId;

            _ = await draftStorage.ReplaceOneAsync(d => d.MatchId == draft.MatchId, draft, new ReplaceOptions() { IsUpsert = true });
            
            // Notifies all players in the match that a character has been banned 
            await Services.Notifications.NotifyPlayer(draft.TeamAPlayerIds.Union(draft.TeamBPlayerIds).ToList(), MatchDraft.NOTIFICATION_CHARACTER_BANNED, draft);
            
            return draft;
        }

        /// <summary>
        /// Called to lock in a character with the given <paramref name="characterId"/> in the given <paramref name="matchId"/>. 
        /// </summary>
        /// <returns>The updated <see cref="MatchDraft"/> state.</returns>
        [ClientCallable]
        public async Task<MatchDraft> SelectCharacter(string matchId, int characterId)
        {
            // Get the id for the player that is making the request
            var requesterId = Context.UserId;

            // Find the match this player is in.
            var draftStorage = await Storage.GetCollection<MatchPreparationStorage, MatchDraft>();
            var queryResults = await draftStorage.FindAsync(matchDraft => matchDraft.MatchId == matchId &&
                                                                     (matchDraft.TeamAPlayerIds.Contains(requesterId) ||
                                                                      matchDraft.TeamBPlayerIds.Contains(requesterId)));

            var draft = queryResults.FirstOrDefault();
            if (draft == null)
                return new MatchDraft() { MatchId = "NOT_FOUND", CurrentPickIdx = -1 };

            // TODO: If whoever downloads this sample wishes to experiment, try adding some guards here against players outside of the match attempting to lock in characters.

            // Get the game type ref
            var draftTypeRef = (DraftSimType)await Services.Content.GetContent(draft.GameTypeId, typeof(DraftSimType));

            var totalNumberOfDraftPicks = draftTypeRef.TotalNumberOfDraftPicks;

            // If the draft is already finished, just return it.
            if (totalNumberOfDraftPicks == draft.CurrentPickIdx)
                return draft;

            // Validate that the requesting player is the one that should be picking right now
            var currTeamToPick = draftTypeRef.PerPickIdxTeams[draft.CurrentPickIdx];
            var currPlayerIdxToPick = draftTypeRef.PerPickIdxPlayerIdx[draft.CurrentPickIdx];

            // Get the idx into the team array for the requesting player and the team of the requesting player.
            draft.GetPlayerAndTeamIndices(requesterId, out var requesterIdx, out var requesterTeam);

            // If the player is not in the match draft or it's not their turn to pick --- just return the draft without updating it.
            if (requesterTeam == -1 || currTeamToPick != requesterTeam ||
                requesterIdx == -1 || currPlayerIdxToPick != requesterIdx)
            {
                return draft;
            }

            // Increment the current pick index cause if we got to this point, we will lock in a character.
            draft.CurrentPickIdx += 1;

            // Ensures that, if someone tries to lock in a banned character, we just pick one at random (TODO: make this throw an error)
            var lockedInCharacters = requesterTeam == 0 ? draft.TeamALockedInCharacters : draft.TeamBLockedInCharacters;
            var bannedCharacters = requesterTeam == 0 ? draft.TeamBBannedCharacters : draft.TeamABannedCharacters;
            if (bannedCharacters.Contains(characterId))
            {
                // If someone tries to select a character that is banned (this shouldn't be possible via the client), we assign them a random character.
                lockedInCharacters[requesterIdx] = new Random().Next(0, 150);
            }
            else
            {
	            // Set the character into the slot and return the draft.
	            lockedInCharacters[requesterIdx] = characterId;
	            _ = await draftStorage.ReplaceOneAsync(d => d.MatchId == draft.MatchId, draft, new ReplaceOptions() {IsUpsert = true});
            }

            // Notifies all players in the match that a character has been selected
            await Services.Notifications.NotifyPlayer(draft.TeamAPlayerIds.Union(draft.TeamBPlayerIds).ToList(), MatchDraft.NOTIFICATION_CHARACTER_SELECTED, draft);

            return draft;
        }
    }
}
