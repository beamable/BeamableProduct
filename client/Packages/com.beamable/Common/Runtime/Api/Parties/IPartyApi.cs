using Beamable.Common;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Parties
{
	public interface IPartyApi
	{
		Promise<PartyQueryResponse> FindParties();

		Promise<Party> JoinParty(string partyId);

		Promise<Party> GetParty(string partyId);

		Promise LeaveParty(string partyId);

		Promise KickPlayer(string lobbyId, string partyId);
	}
}
