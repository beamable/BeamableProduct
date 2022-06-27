using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Parties
{
	public interface IPartyApi
	{
		Promise<PartyQueryResponse> FindParties();

		/// <summary>
		/// Create a new <see cref="Party"/> with the current player as the host.
		/// </summary>
		/// <param name="access">The privacy value for the created party.</param>
		/// <param name="maxPlayers">Configurable value for the maximum number of players this party can have.</param>
		/// <param name="passcodeLength">Configurable value for how long the generated passcode should be.</param>
		/// <returns><see cref="Promise{Party}"/> representing the created party.</returns>
		Promise<Party> CreateParty(PartyAccess access, int? maxPlayers = null, int? passcodeLength = null);

		/// <summary>
		/// Join a <see cref="Party"/> given its id.
		/// </summary>
		/// <param name="partyId">The id of the <see cref="Party"/> to join.</param>
		/// <returns>A <see cref="Promise{Party}"/> representing the joined party.</returns>
		Promise<Party> JoinParty(string partyId);

		/// <summary>
		/// Fetch the current status of a <see cref="Party"/>.
		/// </summary>
		/// <param name="partyId">The id of the <see cref="Party"/>.</param>
		/// <returns>A <see cref="Party"/> representing the fetched party.</returns>
		Promise<Party> GetParty(string partyId);

		/// <summary>
		/// Notify the given party that the player intends to leave.
		/// </summary>
		/// <param name="partyId">The id of the <see cref="Party"/> to leave.</param>
		Promise LeaveParty(string partyId);

		/// <summary>
		/// Send a request to the given <see cref="Party"/> to remove the player with the given playerId.
		/// If the requesting player doesn't have the capability to boot players, this will throw an exception.
		/// </summary>
		/// <param name="lobbyId">The id of the <see cref="Party"/>.</param>
		/// <param name="partyId">The id of the player to remove.</param>
		Promise KickPlayer(string lobbyId, string partyId);
	}
}
