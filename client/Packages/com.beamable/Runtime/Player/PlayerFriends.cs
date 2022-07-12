using Beamable.Common;
using Beamable.Common.Api.Social;
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using System;
using System.Collections.Generic;

namespace Beamable.Player
{
	[Serializable]
	public class PlayerFriends : Observable<SocialList>, IBeamableDisposable
	{
		/// <summary>
		/// Use this to make sure the object is initialized.
		/// </summary>
		public Promise OnReady
		{
			get
			{
				if (IsAssigned)
				{
					return Promise.Success;
				}

				return Refresh();
			}
		}

		/// <summary>
		/// This is a list of players which user added as friends.
		/// </summary>
		public ObservableReadonlyList<Friend> FriendsList { get; private set; }
		/// <summary>
		/// This is a list of players which user blocked.
		/// </summary>
		public ObservableReadonlyList<Common.Api.Social.Player> Blocked { get; private set; }

		private ISocialApi _socialApi;

		public PlayerFriends(ISocialApi socialApi)
		{
			_socialApi = socialApi;
			FriendsList = new ObservableReadonlyList<Friend>(FriendsListRefresh);
			Blocked = new ObservableReadonlyList<Common.Api.Social.Player>(BlockedListRefresh);
		}

		private Promise<List<Friend>> FriendsListRefresh() => Promise<List<Friend>>.Successful(Value?.friends);

		private Promise<List<Common.Api.Social.Player>> BlockedListRefresh() => Promise<List<Common.Api.Social.Player>>.Successful(Value?.blocked);

		protected override async Promise PerformRefresh()
		{
			Value = await _socialApi.Get();
			await FriendsList.Refresh();
			await Blocked.Refresh();
		}

		/// <summary>
		/// Check if player with given id was blocked by the user.
		/// </summary>
		/// <param name="playerId">Id of the player to check.</param>
		/// <returns>True if given player is blocked.</returns>
		public bool IsBlocked(long playerId) => Value.IsBlocked(playerId);

		/// <summary>
		/// Check if player with given id was added to the user's friends list.
		/// </summary>
		/// <param name="playerId">Id of the player to check.</param>
		/// <returns>True if given player is a friend.</returns>
		public bool IsFriend(long playerId) => Value.IsFriend(playerId);

		public Promise OnDispose()
		{
			_socialApi = null;
			return Promise.Success;
		}
	}
}
