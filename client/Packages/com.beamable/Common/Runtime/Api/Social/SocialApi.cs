using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Social
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Friends feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/friends-feature">Notifications</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class SocialApi : ISocialApi
	{
		public IBeamableRequester Requester
		{
			get;
		}

		private IUserContext Ctx
		{
			get;
		}

		private Promise<SocialList> _socialList;

		public SocialApi(IUserContext ctx, IBeamableRequester requester)
		{
			Requester = requester;
			Ctx = ctx;
		}

		public Promise<SocialList> Get()
		{
			if (_socialList == null)
			{
				return RefreshSocialList();
			}

			return _socialList;
		}

		public Promise<EmptyResponse> ImportFriends(SocialThirdParty source, string token)
		{
			return Requester.Request<EmptyResponse>(
				Method.POST,
				"/basic/social/friends/import",
				new ImportFriendsRequest { source = source.GetString(), token = token }
			);
		}

		public Promise<FriendStatus> BlockPlayer(long playerId)
		{
			return Requester.Request<FriendStatus>(
				Method.POST,
				"/basic/social/blocked",
				new PlayerIdRequest { playerId = playerId.ToString() }
			);
		}

		public Promise<FriendStatus> UnblockPlayer(long playerId)
		{
			return Requester.Request<FriendStatus>(
				Method.DELETE,
				"/basic/social/blocked",
				new PlayerIdRequest { playerId = playerId.ToString() }
			);
		}

		// TODO: This needs to be fixed before it can be called.
		// public Promise<EmptyResponse> SendFriendRequest(long gamerTag)
		// {
		//    return Requester.Request<EmptyResponse>(
		//       Method.POST,
		//       "/basic/social/friends/invite",
		//       new GamertagRequest { gt = gamerTag }
		//    );
		// }

		public Promise<EmptyResponse> CancelFriendRequest(long playerId)
		{
			return Requester.Request<EmptyResponse>(
				Method.DELETE,
				"/basic/social/friends/invite",
				new PlayerIdRequest { playerId = playerId.ToString() }
			);
		}

		public Promise<EmptyResponse> RemoveFriend(long playerId)
		{
			return Requester.Request<EmptyResponse>(
				Method.DELETE,
				"/basic/social/friends",
				new PlayerIdRequest { playerId = playerId.ToString() }
			);
		}

		public Promise<SocialList> RefreshSocialList()
		{
			_socialList = Requester.Request<SocialList>(
				Method.GET,
				"/basic/social/my"
			);
			return _socialList;
		}
	}

	[Serializable]
	public class PlayerIdRequest
	{
		public string playerId;
	}

	[Serializable]
	public class ImportFriendsRequest
	{
		public string source;
		public string token;
	}

	[Serializable]
	public class SocialList
	{
		public List<Friend> friends;
		public List<Player> blocked;

		public bool IsBlocked(long dbid)
		{
			return blocked.Find(p => p.playerId == dbid.ToString()) != null;
		}

		public bool IsFriend(long dbid)
		{
			return friends.Find(f => f.playerId == dbid.ToString()) != null;
		}
	}

	[Serializable]
	public class Friend
	{
		public string playerId;
		public string source;

		public FriendSource Source => (FriendSource)Enum.Parse(typeof(FriendSource), source, ignoreCase: true);
	}

	[Serializable]
	public class Player
	{
		public string playerId;
	}

	public enum FriendSource
	{
		Facebook,
		Native
	}

	[Serializable]
	public class FriendStatus
	{
		public bool isBlocked;
	}

	public enum SocialThirdParty
	{
		Facebook
	}

	public static class SocialThirdPartyMethods
	{
		public static string GetString(this SocialThirdParty thirdParty)
		{
			switch (thirdParty)
			{
				case SocialThirdParty.Facebook:
					return "facebook";
				default:
					return null;
			}
		}
	}
}
