using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Presence
{
	public class PresenceService : IPresenceApi
	{
		private readonly IBeamableRequester _requester;
		private readonly IUserContext _userContext;

		public PresenceService(IBeamableRequester requester, IUserContext userContext)
		{
			_requester = requester;
			_userContext = userContext;
		}

		public Promise<EmptyResponse> SendHeartbeat()
		{
			return _requester.Request<EmptyResponse>(
				Method.PUT,
				$"/players/{_userContext.UserId}/presence"
			);
		}

		public Promise<PlayerPresence> GetPlayerPresence(long playerId)
		{
			return _requester.Request<PlayerPresence>(
				Method.GET,
				$"/players/{playerId}/presence"
			);
		}

		public Promise<EmptyResponse> SetPlayerStatus(PresenceStatus status, string description)
		{
			string json = $"{{ \"status\": {(int)status}, \"description\": \"{description}\" }}";

			return _requester.Request<EmptyResponse>(
				Method.PUT,
				$"/players/{_userContext.UserId}/presence/status",
				json
			);
		}

		public Promise<MultiplePlayersStatus> GetManyStatuses(params long[] playerIds)
		{
			string json = $"{{ \"playerIds\": [\"{string.Join("\", \"", playerIds)}\"] }}";

			return _requester.Request<MultiplePlayersStatus>(
				Method.POST,
				"/presence/query",
				json
			);
		}
	}

	[Serializable]
	public class PlayerPresence
	{
		public bool online;
		public string lastOnline;
		public long playerId;
		public int status;
		public string description;

		public PresenceStatus Status => (PresenceStatus)status;
		public DateTime LastOnline => DateTime.Parse(lastOnline);
	}

	// TODO: Remove this once lastOnline format gets unified
	[Serializable]
	public class PlayerPresenceTest
	{
		public bool online;
		public LastOnline lastOnline;
		public long playerId;
		public int status;
		public string description;
	}

	// TODO: Remove this once lastOnline format gets unified
	[Serializable]
	public class LastOnline
	{
		public long seconds;
		public int nanos;
	}

	public enum PresenceStatus
	{
		Online = 0,
		Invisible = 1,
		DoNotDisturb = 2,
		Away = 3,
	}

	[Serializable]
	public class MultiplePlayersStatus
	{
		public List<PlayerPresenceTest> playersStatus;
	}
}
