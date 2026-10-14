using Beamable.Common;
using Beamable.Common.Api.Mail;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Api.Presence;
using Beamable.Common.Api.Social;
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using Beamable.Content.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Player
{
	/// <summary>Manages the current player's friends, blocked players, and invitations.</summary>
	/// <remarks>Await <see cref="OnReady"/> before reading lists. When
	/// <see cref="CoreConfiguration.SuppressAutomaticMailUpdatesFromPlayerSocial"/> is enabled, sending and
	/// accepting invitations requires <see cref="EnableFriendInvitationMailChecks"/> for this context.
	/// Inherited <c>OnUpdated</c> notifications expose changes; detach consumer handlers when no longer needed.</remarks>
	[Serializable]
	public class PlayerSocial : DefaultObservable, IBeamableDisposable
	{
		private const string SOCIAL_UPDATE_CHANNEL = "SOCIAL.UPDATE";
		private const string MAIL_UPDATE_CHANNEL = "MAILBOX.UPDATE";
		private const string MAIL_SOCIAL_INVITE_CATEGORY = "SOCIAL.FRIEND.INVITE";
		private const string FRIEND_PRESENCE_CHANGED = "social.friend_presence_changed";
		private const string MAIL_SUPPRESSION_MESSAGE =
			"Friend invitation Mail checks are disabled for this player context. " +
			"Call EnableFriendInvitationMailChecks before sending or accepting friend invitations.";

		/// <summary>Gets the promise for the initial social refresh and any enabled invitation-mail checks.</summary>
		/// <value>A promise that completes after initial loading, or rejects on loading failure.</value>
		/// <remarks>Mail checks are omitted when suppressed by configuration. Call
		/// <see cref="EnableFriendInvitationMailChecks"/> before invitation operations in that case.</remarks>
		public Promise OnReady { get; private set; }

		/// <summary>The current player's observable list of friends.</summary>
		/// <remarks>Await <see cref="OnReady"/> before reading. Send invitations with <see cref="Invite"/>
		/// and accept incoming invitations with <see cref="AcceptInviteFrom"/>.</remarks>
		public PlayerFriendList Friends;

		/// <summary>
		/// A list of <see cref="BlockedPlayer"/>s for the current player. Blocked players cannot send this player friend invites.
		/// A player can block players with the <see cref="BlockPlayer"/> method.
		/// </summary>
		public BlockedPlayerList Blocked;

		/// <summary>
		/// A list of <see cref="SentFriendInvite"/>s that the current player as issued.
		/// New invites can be made with the <see cref="Invite"/> method, and invitations can be cancelled with the <see cref="CancelInvite"/>
		/// </summary>
		public SentFriendInviteList SentInvites;

		/// <summary>The incoming friend invitations known to this context.</summary>
		/// <remarks>Await <see cref="EnableFriendInvitationMailChecks"/> to ensure invitation mail is loaded.
		/// Use an invitation's <see cref="ReceivedFriendInvite.invitingPlayerId"/> with <see cref="AcceptInviteFrom"/>.</remarks>
		public ReceivedFriendInviteList ReceivedInvites;

		/// <summary>
		/// An event which fires when any of the player's friends changes presence status. 
		/// </summary>
		public event Action<PlayerFriend> FriendPresenceChanged;

		private bool _friendInvitationMailChecksEnabled;
		private bool _mailUpdateSubscribed;
		private SocialList _socialList;

		private ISocialApi _socialApi;
		private List<MailMessage> _inviteMail;
		private readonly INotificationService _notificationService;
		private readonly IMailApi _mailApi;
		private readonly IPresenceApi _presenceApi;

		public PlayerSocial(ISocialApi socialApi, INotificationService notificationService, IMailApi mailApi, IPresenceApi presenceApi)
		{
			_socialApi = socialApi;
			_notificationService = notificationService;
			_mailApi = mailApi;
			_presenceApi = presenceApi;
			_inviteMail = new List<MailMessage>();

			_friendInvitationMailChecksEnabled =
				!CoreConfiguration.Instance.SuppressAutomaticMailUpdatesFromPlayerSocial;

			Friends = new PlayerFriendList(FriendsListRefresh);
			Blocked = new BlockedPlayerList(BlockedListRefresh);
			SentInvites = new SentFriendInviteList(SentInviteRefresh);
			ReceivedInvites = new ReceivedFriendInviteList(ReceivedInviteRefresh);

			// lots of events will show up on the social update channel
			_notificationService.Subscribe<FriendRequestUpdateNotification>(SOCIAL_UPDATE_CHANNEL, OnSocialUpdate);

			if (_friendInvitationMailChecksEnabled)
			{
				SubscribeToMailUpdates();
			}
			else
			{
				Debug.LogWarning(MAIL_SUPPRESSION_MESSAGE);
			}

			_notificationService.Subscribe<FriendStatusChangedNotification>(FRIEND_PRESENCE_CHANGED, OnFriendPresenceChanged);

			OnReady = Refresh().FlatMap(_ => RefreshMail()).ToPromise();
		}

		// Subscribe to mail updates while preventing duplicate subscriptions.
		private void SubscribeToMailUpdates()
		{
			if (_mailUpdateSubscribed) return;

			// Invitation mail IDs arrive through the mail notification channel.
			_notificationService.Subscribe(MAIL_UPDATE_CHANNEL, OnMailUpdate);
			_mailUpdateSubscribed = true;
		}

		private void UnsubscribeFromMailUpdates()
		{
			if (!_mailUpdateSubscribed) return;
			_notificationService.Unsubscribe(MAIL_UPDATE_CHANNEL, OnMailUpdate);
			_mailUpdateSubscribed = false;
		}

		private async void OnFriendPresenceChanged(FriendStatusChangedNotification notification)
		{
			// TODO: [TD000007] Use information from the notification instead of requesting player presence
			var presence = await _presenceApi.GetPlayerPresence(notification.friendId);
			var player = Friends.FirstOrDefault(friend => friend.playerId == notification.friendId);
			if (player != null)
			{
				player.Presence = presence;
			}
		}

		private void OnMailUpdate(object _)
		{
			var __ = RefreshMail();
		}

		private async Promise RefreshMail()
		{
			if (!_friendInvitationMailChecksEnabled) return;

			var inviteMail = await _mailApi.GetMail(MAIL_SOCIAL_INVITE_CATEGORY);
			_inviteMail = inviteMail.result;

			await ReceivedInvites.Refresh();
			TriggerUpdate();
		}

		private void OnSocialUpdate(FriendRequestUpdateNotification _)
		{
			// TODO: I think we could we use the contents of the notification to be smarter... But for now, lets just blast and refresh everything...
			var __ = Refresh();
		}

		private async Promise<List<PlayerFriend>> FriendsListRefresh()
		{
			var playerIds = _socialList.friends.Select(friend => long.Parse(friend.playerId)).ToArray();
			var statuses = await _presenceApi.GetManyStatuses(playerIds);

			var friends = new List<PlayerFriend>(_socialList.friends.Count);
			foreach (var friend in _socialList.friends)
			{
				var currentStatus = statuses.playersStatus.FirstOrDefault(status => status.playerId.ToString() == friend.playerId);
				friends.Add(new PlayerFriend(this, currentStatus, player => FriendPresenceChanged?.Invoke(player))
				{
					playerId = long.Parse(friend.playerId)
				});
			}

			return friends;
		}

		private Promise<List<BlockedPlayer>> BlockedListRefresh()
		{
			var blocked = new List<BlockedPlayer>(_socialList.blocked.Count);
			foreach (var block in _socialList.blocked)
			{
				blocked.Add(new BlockedPlayer(this) { playerId = long.Parse(block.playerId) });
			}

			return Promise<List<BlockedPlayer>>.Successful(blocked);
		}

		private Promise<List<SentFriendInvite>> SentInviteRefresh()
		{
			var invites = new List<SentFriendInvite>();
			foreach (var invite in _socialList.invites)
			{
				if (invite.Direction != FriendInviteDirection.Outgoing) continue;
				invites.Add(new SentFriendInvite(this) { invitedPlayerId = invite.playerId });
			}
			return Promise<List<SentFriendInvite>>.Successful(invites);
		}

		private Promise<List<ReceivedFriendInvite>> ReceivedInviteRefresh()
		{
			var invites = new Dictionary<long, ReceivedFriendInvite>();

			foreach (var invite in _socialList.invites)
			{
				if (invite.Direction != FriendInviteDirection.Incoming) continue;

				if (!invites.ContainsKey(invite.playerId))
				{
					invites.Add(invite.playerId, new ReceivedFriendInvite(this) { invitingPlayerId = invite.playerId });
				}
			}

			foreach (var mail in _inviteMail)
			{
				if (!invites.TryGetValue(mail.senderGamerTag, out var receivedInvite))
				{
					receivedInvite = new ReceivedFriendInvite(this) { invitingPlayerId = mail.senderGamerTag, };
				}

				receivedInvite.mailId = mail.id;
				invites[mail.senderGamerTag] = receivedInvite;
			}

			return Promise<List<ReceivedFriendInvite>>.Successful(invites.Values.ToList());
		}

		/// <summary>
		/// Refresh the state of the social service
		/// </summary>
		public async Promise Refresh()
		{
			_socialList = await _socialApi.RefreshSocialList();

			await SentInvites.Refresh();
			await ReceivedInvites.Refresh();
			await Friends.Refresh();
			await Blocked.Refresh();

			TriggerUpdate();
		}

		public override int GetBroadcastChecksum()
		{
			return (
					Friends.GetBroadcastChecksum(),
					Blocked.GetBroadcastChecksum(),
					SentInvites.GetBroadcastChecksum(),
					ReceivedInvites.GetBroadcastChecksum())
				.GetHashCode();
		}

		/// <summary>
		/// Check if player with given id was blocked by the user.
		/// </summary>
		/// <param name="playerId">Id of the player to check.</param>
		/// <returns>True if given player is blocked.</returns>
		public bool IsBlocked(long playerId) => _socialList.IsBlocked(playerId);

		/// <summary>
		/// Check if player with given id was added to the user's friends list.
		/// </summary>
		/// <param name="playerId">Id of the player to check.</param>
		/// <returns>True if given player is a friend.</returns>
		public bool IsFriend(long playerId) => _socialList.IsFriend(playerId);

		/// <summary>Sends a friend invitation to another player and refreshes social data.</summary>
		/// <param name="playerId">The recipient's Beamable gamer tag, not an email or local player code.</param>
		/// <returns>A promise that completes after the request and refresh; request or refresh failures reject it.</returns>
		/// <exception cref="InvalidOperationException">Invitation mail checks are disabled for this context.</exception>
		/// <remarks>Await <see cref="EnableFriendInvitationMailChecks"/> first if checks may be suppressed.
		/// The recipient accepts using <see cref="AcceptInviteFrom"/>. Use <see cref="CancelInvite"/> to cancel.</remarks>
		/// <example>
		/// <para>Example: send an invitation to a known gamer tag, enabling checks for this context.</para>
		/// <code>
		/// using System;
		/// using System.Threading.Tasks;
		/// using Beamable;
		/// using UnityEngine;
		///
		/// public static class SendFriendInviteExample
		/// {
		///     public static async Task Run(long recipientGamerTag)
		///     {
		///         try
		///         {
		///             var context = await BeamContext.Default.Instance;
		///             await context.Social.EnableFriendInvitationMailChecks();
		///             await context.Social.Invite(recipientGamerTag);
		///         }
		///         catch (Exception exception)
		///         {
		///             Debug.LogException(exception);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		public async Promise Invite(long playerId)
		{
			if (!_friendInvitationMailChecksEnabled)
			{
				throw new InvalidOperationException(MAIL_SUPPRESSION_MESSAGE);
			}

			await _socialApi.SendFriendRequest(playerId);
			await Refresh();
		}

		/// <summary>Accepts a known incoming friend invitation and refreshes social data.</summary>
		/// <param name="playerId">The inviting player's gamer tag from <see cref="ReceivedInvites"/>.</param>
		/// <returns>A promise that completes after acceptance, mail update, and social refresh.</returns>
		/// <exception cref="InvalidOperationException">Invitation mail checks are disabled for this context.</exception>
		/// <exception cref="Exception">No invitation or associated invitation mail is known for the player.</exception>
		/// <remarks>Enable mail checks first. Request and refresh failures also reject the promise.</remarks>
		/// <example>
		/// <para>Example: accept an invitation from a known sender on the recipient device or context.</para>
		/// <code>
		/// using System;
		/// using System.Linq;
		/// using System.Threading.Tasks;
		/// using Beamable;
		/// using UnityEngine;
		///
		/// public static class AcceptFriendInviteExample
		/// {
		///     public static async Task Run(long senderGamerTag)
		///     {
		///         try
		///         {
		///             var context = await BeamContext.Default.Instance;
		///             var social = context.Social;
		///             await social.EnableFriendInvitationMailChecks();
		///             await social.Refresh();
		///             var invite = social.ReceivedInvites.FirstOrDefault(
		///                 entry =&gt; entry.invitingPlayerId == senderGamerTag);
		///             if (invite == null)
		///             {
		///                 Debug.LogWarning("No invitation from this player is available yet.");
		///                 return;
		///             }
		///             await social.AcceptInviteFrom(invite.invitingPlayerId);
		///         }
		///         catch (Exception exception)
		///         {
		///             Debug.LogException(exception);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		public async Promise AcceptInviteFrom(long playerId)
		{
			if (!_friendInvitationMailChecksEnabled)
			{
				throw new InvalidOperationException(MAIL_SUPPRESSION_MESSAGE);
			}

			await OnReady;
			var invite = ReceivedInvites.FirstOrDefault(i => i.invitingPlayerId == playerId);
			if (invite == null)
			{
				throw new Exception("No invite from " + playerId);
			}

			if (invite.mailId == 0)
			{
				throw new Exception("No mail known");
			}

			var mailUpdateReq = new MailUpdateRequest();
			foreach (var mail in _inviteMail)
			{
				if (mail.senderGamerTag != playerId) continue;
				mailUpdateReq.Add(mail.id, MailState.Read, false, DateTime.UtcNow.ToString(DateUtility.ISO_FORMAT));
			}
			_inviteMail.Clear();
			var mailUpdate = _mailApi.Update(mailUpdateReq);
			await _socialApi.AcceptFriendRequest(playerId);
			await mailUpdate;
			await Refresh();
		}

		/// <summary>
		/// Blocks the player with the given <see cref="playerId"/>.
		/// When a player is blocked, they cannot send friend requests to the current player.
		/// A player can be unblocked with the <see cref="UnblockPlayer"/> method.
		/// After the resultant <see cref="Promise"/> completes, the <see cref="Blocked"/> field will include the given <see cref="playerId"/>
		/// </summary>
		/// <param name="playerId">the player id of the player to block</param>
		public async Promise BlockPlayer(long playerId)
		{
			await OnReady;

			await _socialApi.BlockPlayer(playerId);
			await Refresh();
		}

		/// <summary>
		/// Remove the friend with the given <see cref="playerId"/> from the <see cref="Friends"/> list.
		/// The other player will receive a notification that the friendship has been dissolved, and this player will be removed from their <see cref="Friends"/> list as well.
		/// You cannot unfriend a player who is not already in your friends list.
		/// After the resultant <see cref="Promise"/> completes, the <see cref="Friends"/> field will no longer include the given <see cref="playerId"/>
		/// </summary>
		/// <param name="playerId">the gamerTag of the player to unfriend.</param>
		public async Promise Unfriend(long playerId)
		{
			await _socialApi.RemoveFriend(playerId);
			await Refresh();
		}

		/// <summary>
		/// Unblocks a player with the given <see cref="playerId"/>.
		/// You cannot unblock a player who is not currently blocked. You can block players with the <see cref="Blocked"/> method.
		/// After the resultant <see cref="Promise"/> completes, the <see cref="Blocked"/> field will no longer include the given <see cref="playerId"/>
		/// </summary>
		/// <param name="playerId">the player id of the player to unblock</param>
		public async Promise UnblockPlayer(long playerId)
		{
			await OnReady;

			await _socialApi.UnblockPlayer(playerId);
			await Refresh();
		}

		/// <summary>
		/// After a player has sent an invite with the <see cref="Invite"/> method, the player can cancel the invitation and the invited player
		/// will not be able to become friends any more.
		/// After the resultant <see cref="Promise"/> completes, the <see cref="SentInvites"/> list will no longer include the given <see cref="playerId"/>
		/// </summary>
		/// <param name="playerId">the player id of the player to cancel the friendship request with</param>
		public async Promise CancelInvite(long playerId)
		{
			await OnReady;

			await _socialApi.CancelFriendRequest(playerId);
			await Refresh();
		}

		/// <summary>
		/// Import friends from Facebook.
		/// </summary>
		/// <param name="thirdPartyAuthToken">
		/// An access token issued from Facebook that can be sent to Beamable so that the Beamable Cloud can perform the friend import.
		/// </param>
		/// <returns>A <see cref="Promise"/> representing the network call.</returns>
		public async Promise ImportFacebookFriends(string thirdPartyAuthToken)
		{
			await OnReady;

			await _socialApi.ImportFriends(SocialThirdParty.Facebook, thirdPartyAuthToken);
			await Refresh();
		}

		/// <summary>Enables and loads the mailbox checks needed for friend invitations in this context.</summary>
		/// <returns>A promise that completes when mail checks are enabled and loaded, or rejects on failure.</returns>
		/// <remarks>This waits for <see cref="OnReady"/>, subscribes to future mail updates, and loads invitation mail.
		/// It is safe to call when already enabled. If enabling fails, the new mail subscription is removed.
		/// This enables checks only for this context and does not change the global suppression setting.</remarks>
		/// <seealso cref="Invite"/>
		public async Promise EnableFriendInvitationMailChecks()
		{
			await OnReady;

			// Recheck because another caller may have enabled it while awaiting.
			if (_friendInvitationMailChecksEnabled) return;

			_friendInvitationMailChecksEnabled = true;
			SubscribeToMailUpdates();

			try
			{
				await RefreshMail();
			}
			catch
			{
				_friendInvitationMailChecksEnabled = false;
				UnsubscribeFromMailUpdates();
				throw;
			}
		}

		public Promise OnDispose()
		{
			_notificationService.Unsubscribe<FriendRequestUpdateNotification>(SOCIAL_UPDATE_CHANNEL, OnSocialUpdate);
			UnsubscribeFromMailUpdates();
			_notificationService.Unsubscribe<FriendStatusChangedNotification>(FRIEND_PRESENCE_CHANGED, OnFriendPresenceChanged);
			_socialApi = null;
			return Promise.Success;
		}
	}


	[Serializable]
	public class PlayerFriendList : ObservableReadonlyList<PlayerFriend>
	{
		public PlayerFriendList(Func<Promise<List<PlayerFriend>>> refreshFunction) : base(refreshFunction) { }
	}

	/// <summary>
	/// a friend of the player
	/// </summary>
	[Serializable]
	public class PlayerFriend
	{
		/// <summary>
		/// the player id of the friend
		/// </summary>
		public long playerId;

		/// <summary>
		/// The current <see cref="PlayerPresence"/> status of the player.
		/// </summary>
		public PlayerPresence Presence
		{
			get => _presence;
			set
			{
				_presence = value;
				_onPresenceUpdated?.Invoke(this);
			}
		}
		private PlayerPresence _presence;
		private Action<PlayerFriend> _onPresenceUpdated;

		private readonly PlayerSocial _sdk;

		internal PlayerFriend(PlayerSocial sdk, PlayerPresence presenceStatus, Action<PlayerFriend> onPresenceUpdated)
		{
			_sdk = sdk;
			_presence = presenceStatus;
			_onPresenceUpdated = onPresenceUpdated;
		}

		/// <summary>
		/// Blocks the friend
		/// </summary>
		public Promise Block()
		{
			return _sdk.BlockPlayer(playerId);
		}

		/// <summary>
		/// Remove the friendship with the player
		/// </summary>
		public Promise Unfriend()
		{
			return _sdk.Unfriend(playerId);
		}


		#region auto-generated-equality
		protected bool Equals(PlayerFriend other)
		{
			return playerId == other.playerId && Equals(_presence, other._presence);
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

			return Equals((PlayerFriend)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (playerId.GetHashCode() * 397) ^ (_presence != null ? _presence.GetHashCode() : 0);
			}
		}
		#endregion
	}

	[Serializable]
	public class BlockedPlayerList : ObservableReadonlyList<BlockedPlayer>
	{
		public BlockedPlayerList(Func<Promise<List<BlockedPlayer>>> refreshFunction) : base(refreshFunction) { }
	}

	/// <summary>
	/// A player that the current player has blocked
	/// </summary>
	[Serializable]
	public class BlockedPlayer
	{
		/// <summary>
		/// The player id of the blocked player
		/// </summary>
		public long playerId;
		private readonly PlayerSocial _sdk;

		internal BlockedPlayer(PlayerSocial sdk)
		{
			_sdk = sdk;
		}

		/// <summary>
		/// Unblock the player.
		/// </summary>
		public Promise Unblock()
		{
			return _sdk.UnblockPlayer(playerId);
		}

		#region auto-generated-equality
		protected bool Equals(BlockedPlayer other)
		{
			return playerId == other.playerId;
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

			return Equals((BlockedPlayer)obj);
		}

		public override int GetHashCode()
		{
			return playerId.GetHashCode();
		}
		#endregion
	}

	[Serializable]
	public class SentFriendInviteList : ObservableReadonlyList<SentFriendInvite>
	{
		public SentFriendInviteList(Func<Promise<List<SentFriendInvite>>> refreshFunction) : base(refreshFunction) { }
	}

	/// <summary>
	/// A friend invite that the current player has sent to another player
	/// </summary>
	[Serializable]
	public class SentFriendInvite
	{
		#region auto-generated-equality
		protected bool Equals(SentFriendInvite other)
		{
			return invitedPlayerId == other.invitedPlayerId;
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

			return Equals((SentFriendInvite)obj);
		}

		public override int GetHashCode()
		{
			return invitedPlayerId.GetHashCode();
		}
		#endregion

		/// <summary>
		/// The id of the player that this player invited
		/// </summary>
		public long invitedPlayerId;

		private readonly PlayerSocial _sdk;

		internal SentFriendInvite(PlayerSocial sdk)
		{
			_sdk = sdk;
		}

		/// <summary>
		/// Cancel the friend invite. The invited player will not be able to accept the invite after it is cancelled, and
		/// the invite will be removed from their receivable invites.
		/// </summary>
		public Promise Cancel()
		{
			return _sdk.CancelInvite(invitedPlayerId);
		}
	}

	[Serializable]
	public class ReceivedFriendInviteList : ObservableReadonlyList<ReceivedFriendInvite>
	{
		public ReceivedFriendInviteList(Func<Promise<List<ReceivedFriendInvite>>> refreshFunction) : base(refreshFunction) { }
	}

	/// <summary>
	/// An invitation sent by another player to request friendship with the current player.
	/// Use the <see cref="AcceptInvite"/> method to accept the invite, or the <see cref="BlockSender"/> method to reject.
	/// </summary>
	[Serializable]
	public class ReceivedFriendInvite
	{
		#region autogenerated-equality-members
		protected bool Equals(ReceivedFriendInvite other)
		{
			return invitingPlayerId == other.invitingPlayerId && mailId == other.mailId;
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

			return Equals((ReceivedFriendInvite)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (invitingPlayerId.GetHashCode() * 397) ^ mailId.GetHashCode();
			}
		}
		#endregion

		/// <summary>
		/// The id of the player that invited this player
		/// </summary>
		public long invitingPlayerId;

		/// <summary>
		/// The id of the mail that was issued to notify the player of the invite
		/// </summary>
		public long mailId;

		private readonly PlayerSocial _sdk;

		internal ReceivedFriendInvite(PlayerSocial sdk)
		{
			_sdk = sdk;
		}

		/// <summary>
		/// Accept the invitation and become friends with the sender
		/// </summary>
		public Promise AcceptInvite()
		{
			return _sdk.AcceptInviteFrom(invitingPlayerId);
		}

		/// <summary>
		/// Reject this invitation by blocking the sender
		/// </summary>
		public Promise BlockSender()
		{
			return _sdk.BlockPlayer(invitingPlayerId);
		}
	}

}
