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
		public string status;
		public string description;

		public PresenceStatus Status => (PresenceStatus)Enum.Parse(typeof(PresenceStatus), status);
		public DateTime LastOnline => DateTime.Parse(lastOnline);
		
		#region auto-generated-equality
		protected bool Equals(PlayerPresence other)
		{
			return online == other.online && lastOnline == other.lastOnline && playerId == other.playerId && status == other.status && description == other.description;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((PlayerPresence) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = online.GetHashCode();
				hashCode = (hashCode * 397) ^ (lastOnline != null ? lastOnline.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ playerId.GetHashCode();
				hashCode = (hashCode * 397) ^ (status != null ? status.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (description != null ? description.GetHashCode() : 0);
				return hashCode;
			}
		}
		#endregion
	}

	public enum PresenceStatus
	{
		Visible = 0,
		Invisible = 1,
		Dnd = 2,
		Away = 3,
	}

	[Serializable]
	public class MultiplePlayersStatus
	{
		public List<PlayerPresence> playersStatus;
	}

	[Serializable]
	public class FriendStatusChangedNotification
	{
		// TODO: [TD000007] Change those fields according to the new notification structure
		public long friendId;
		public string onlineStatus;
		public string lastOnline;
		public string description;
		
		public DateTime LastOnline => DateTime.Parse(lastOnline);
	}
}
