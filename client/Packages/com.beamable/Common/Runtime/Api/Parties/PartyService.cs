using Beamable.Common;
using Beamable.Common.Api;
using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class InvitesResponse
	{
		public List<PartyInvite> invitations;
	}
	
	[Serializable]
	public struct PartyInvite
	{
		public string partyId, invitedBy;
	}
	
	public class PartyService : IPartyApi
	{
		private readonly IBeamableRequester _requester;
		private readonly IUserContext _userContext;

		public PartyService(IBeamableRequester requester, IUserContext userContext)
		{
			_requester = requester;
			_userContext = userContext;
		}

		public Promise<Party> CreateParty(PartyRestriction restriction, int maxSize = 0)
		{
			object request;
			if (maxSize == 0)
			{
				request = new CreateDefaultPartyRequest(restriction.ToString(), _userContext.UserId.ToString());
			}
			else
			{
				request = new CreatePartyRequest(restriction.ToString(), _userContext.UserId.ToString(), maxSize);
			}
			
			return _requester.Request<Party>(
				Method.POST,
				"/parties",
				request
			);
		}

		public Promise<Party> UpdateParty(string partyId, PartyRestriction restriction, int maxSize = 0)
		{
			object request;
			if (maxSize == 0)
			{
				request = new UpdateDefaultPartyRequest(restriction.ToString());
			}
			else
			{
				request = new UpdatePartyRequest(restriction.ToString(), maxSize);
			}
			
			return _requester.Request<Party>(
				Method.PUT,
				$"/parties/{partyId}/metadata",
				request
			);
		}

		public Promise<Party> JoinParty(string partyId)
		{
			return _requester.Request<Party>(
				Method.PUT,
				$"/parties/{partyId}"
			);
		}

		public Promise<Party> GetParty(string partyId)
		{
			return _requester.Request<Party>(
				Method.GET,
				$"/parties/{partyId}"
			);
		}

		public Promise LeaveParty(string partyId)
		{
			return _requester.Request<Unit>(
				Method.DELETE,
				$"/parties/{partyId}/members",
				new PlayerRequest(_userContext.UserId.ToString())
			).ToPromise();
		}

		public Promise KickPlayer(string partyId, string playerId)
		{
			return _requester.Request<Unit>(
				Method.DELETE,
				$"/parties/{partyId}/members",
				new PlayerRequest(playerId)
			).ToPromise();
		}

		public Promise PromoteToLeader(string partyId, string playerId)
		{
			return _requester.Request<Unit>(
				Method.PUT,
				$"/parties/{partyId}/promote",
				new PlayerRequest(playerId)
			).ToPromise();
		}
		
		public Promise InviteToParty(string partyId, string playerId)
		{
			return _requester.Request<Unit>(
				Method.POST,
				$"/parties/{partyId}/invite",
				new PlayerRequest(playerId)
			).ToPromise();
		}

		public Promise<InvitesResponse> GetPartyInvites()
		{
			return _requester.Request<InvitesResponse>(
				Method.GET,
				$"/players/{_userContext.UserId}/parties/invites"
			);
		}
		
		public Promise KickPlayer(string partyId, long playerId) => KickPlayer(partyId, playerId.ToString());

		public Promise PromoteToLeader(string partyId, long playerId) => PromoteToLeader(partyId, playerId.ToString());

		public Promise InviteToParty(string partyId, long playerId) => InviteToParty(partyId, playerId.ToString());
	}
}
