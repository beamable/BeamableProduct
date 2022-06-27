using Beamable.Common;
using Beamable.Common.Api;

namespace Beamable.Experimental.Api.Parties
{
	public class PartyService : IPartyApi
	{
		private readonly IBeamableRequester _requester;
		private readonly IUserContext _userContext;

		public PartyService(IBeamableRequester requester, IUserContext userContext)
		{
			_requester = requester;
			_userContext = userContext;
		}
		
		public Promise<PartyQueryResponse> FindParties()
		{
			throw new System.NotImplementedException();
		}

		public Promise<Party> CreateParty(PartyAccess access, int? maxPlayers = null, int? passcodeLength = null)
		{
			return _requester.Request<Party>(Method.POST, "/parties",
			                                 new CreatePartyRequest(access.ToString(), maxPlayers));
		}

		public Promise<Party> JoinParty(string partyId)
		{
			return null;
		}

		public Promise<Party> GetParty(string partyId)
		{
			throw new System.NotImplementedException();
		}

		public Promise LeaveParty(string partyId)
		{
			throw new System.NotImplementedException();
		}

		public Promise KickPlayer(string lobbyId, string partyId)
		{
			throw new System.NotImplementedException();
		}
	}
}
