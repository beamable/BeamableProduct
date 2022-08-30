using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Player;
using Beamable.Experimental.Api.Parties;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;

namespace Beamable.Player
{
	/// <summary>
	/// Experimental API around managing a player's party state.
	/// </summary>
	[Serializable]
	public class PlayerParty : Observable<Party>, IDisposable
	{
		private readonly IPartyApi _partyApi;
		private readonly INotificationService _notificationService;
		private readonly IUserContext _userContext;
		private Party _state;
		private Action<object, object> _onPlayerJoined;
		private Action<object, object> _onPlayerLeft;
		private Action<object> _onPlayerInvited;
		private Action<object, long, long, string, string> _onPartyUpdated;
		private Action<object, object> _onPlayerPromoted;
		private Action<object, object> _onPlayerKicked;

		public PlayerParty(IPartyApi partyApi, INotificationService notificationService, IUserContext userContext)
		{
			_partyApi = partyApi;
			_notificationService = notificationService;
			_userContext = userContext;
			Members = new ObservableReadonlyList<string>(RefreshMembersList);
		}

		private static string PlayersLeftName(string partyId) => $"party.players_left.{partyId}";
		private static string PlayersJoinedName(string partyId) => $"party.players_joined.{partyId}";
		private static string PlayerInvitedName(string partyId) => $"party.players_invited.{partyId}";
		private static string PartyUpdatedName(string partyId) => $"party.updated.{partyId}";
		private static string PlayerPromotedName(string partyId) => $"party.player_promoted_to_leader.{partyId}";
		private static string PlayerKickedName(string partyId) => $"party.player_kicked.{partyId}";

		public override Party Value
		{
			get => base.Value;
			set
			{
				if (value != null)
				{
					if (_state == null)
					{
						_notificationService.Subscribe(PlayersLeftName(value.id), PlayerLeft);
						_notificationService.Subscribe(PlayersJoinedName(value.id), PlayerJoined);
						_notificationService.Subscribe(PlayerInvitedName(value.id), PlayerInvited);
						_notificationService.Subscribe(PartyUpdatedName(value.id), PartyUpdated);
						_notificationService.Subscribe(PlayerPromotedName(value.id), PlayerPromoted);
						_notificationService.Subscribe(PlayerKickedName(value.id), PlayerKicked);
					}
				}
				else
				{
					if (_state != null)
					{
						_notificationService.Unsubscribe(PlayersLeftName(_state.id), PlayerLeft);
						_notificationService.Unsubscribe(PlayersJoinedName(_state.id), PlayerJoined);
						_notificationService.Unsubscribe(PlayerInvitedName(_state.id), PlayerInvited);
						_notificationService.Unsubscribe(PartyUpdatedName(_state.id), PartyUpdated);
						_notificationService.Unsubscribe(PlayerPromotedName(_state.id), PlayerPromoted);
						_notificationService.Unsubscribe(PlayerKickedName(_state.id), PlayerKicked);
					}
				}

				if (_state == null)
				{
					_state = value;
				}
				else
				{
					_state.Set(value);
				}

				base.Value = value;
			}
		}

		private async void PlayerJoined(object data)
		{
			await Refresh();
			
			object partyId = null, playerId = null;
			if (data is ArrayDict dict)
			{
				partyId = dict["partyId"];
				playerId = dict["playerThatJoinedId"];
			}
			
			_onPlayerJoined?.Invoke(partyId, playerId);
		}

		private async void PlayerLeft(object data)
		{
			await Refresh();
			
			object partyId = null, playerId = null;
			if (data is ArrayDict dict)
			{
				partyId = dict["partyId"];
				playerId = dict["playerThatLeftId"];
			}
			
			_onPlayerLeft?.Invoke(partyId, playerId);
		}

		private void PlayerInvited(object data)
		{
			_onPlayerInvited?.Invoke(data);
		}

		private async void PartyUpdated(object data)
		{
			await Refresh();
			
			object partyId = null;
			long oldMaxSize = 0, newMaxSize = 0;
			string oldRestriction = "", newRestriction = "";
			if (data is ArrayDict dict)
			{
				partyId = dict["partyId"];
				oldMaxSize = long.Parse(dict["oldMaxSize"].ToString());
				newMaxSize = long.Parse(dict["newMaxSize"].ToString());
				oldRestriction = dict["oldRestriction"].ToString();
				newRestriction = dict["newRestriction"].ToString();
			}
			
			_onPartyUpdated?.Invoke(partyId, oldMaxSize, newMaxSize, oldRestriction, newRestriction);
		}

		private async void PlayerPromoted(object data)
		{
			await Refresh();

			object partyId = null, playerId = null;
			if (data is ArrayDict dict)
			{
				partyId = dict["partyId"];
				playerId = dict["playerPromotedId"];
			}
			
			_onPlayerPromoted?.Invoke(partyId, playerId);
		}

		private async void PlayerKicked(object data)
		{
			await Refresh();
			
			object partyId = null, playerId = null;
			if (data is ArrayDict dict)
			{
				partyId = dict["partyId"];
				playerId = dict["kickedPlayerId"];
			}
			
			_onPlayerKicked?.Invoke(partyId, playerId);
		}

		private Promise<List<string>> RefreshMembersList() => Promise<List<string>>.Successful(_state.members);

		protected override async Promise PerformRefresh()
		{
			if (State == null) return;

			State = await _partyApi.GetParty(State.id);
			await Members.Refresh();
		}

		/// <summary>
		/// The current <see cref="Party"/> the player is in. If the player is not in a party, then this field is null.
		/// A player can only be in one party at a time.
		/// </summary>
		public Party State
		{
			get => _state;
			private set => Value = value;
		}

		/// <summary>
		/// Checks if the player is in a party.
		/// </summary>
		public bool IsInParty => State != null && Value != null;

		/// <inheritdoc cref="Party.id"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public string Id => SafeAccess(State?.id);

		/// <inheritdoc cref="Party.Restriction"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public PartyRestriction Restriction => SafeAccess(State.Restriction);

		/// <inheritdoc cref="Party.leader"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public string Leader => SafeAccess(State?.leader);

		/// <summary>
		/// This property checks if the current player is a party leader.
		/// </summary>
		public bool IsLeader => SafeAccess(State?.leader).Equals(_userContext.UserId.ToString());

		/// <inheritdoc cref="Party.members"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public ObservableReadonlyList<string> Members { get; private set; }

		/// <inheritdoc cref="Party.maxSize"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public int MaxSize => SafeAccess(State?.maxSize) ?? 0;

		private T SafeAccess<T>(T value)
		{
			if (!IsInParty)
			{
				throw new NotInParty();
			}

			return value;
		}

		public void RegisterCallbacks(Action<object, object> onPlayerJoined,
		                              Action<object, object> onPlayerLeft,
		                              Action<object> onPlayerInvited,
		                              Action<object, long, long, string, string> onPartyUpdated,
		                              Action<object, object> onPlayerPromoted,
		                              Action<object, object> onPlayerKicked)
		{
			_onPlayerJoined = onPlayerJoined;
			_onPlayerLeft = onPlayerLeft;
			_onPlayerInvited = onPlayerInvited;
			_onPartyUpdated = onPartyUpdated;
			_onPlayerPromoted = onPlayerPromoted;
			_onPlayerKicked = onPlayerKicked;
		}

		/// <inheritdoc cref="IPartyApi.CreateParty"/>
		public async Promise Create(PartyRestriction restriction,
		                            int maxSize,
		                            Action<object, object> onPlayerJoined = null,
		                            Action<object, object> onPlayerLeft = null,
		                            Action<object> onPlayerInvited = null,
		                            Action<object, long, long, string, string> onPartyUpdated = null,
		                            Action<object, object> onPlayerPromoted = null,
		                            Action<object, object> onPlayerKicked = null)
		{
			State = await _partyApi.CreateParty(restriction, maxSize);
			await Members.Refresh();
			RegisterCallbacks(onPlayerJoined, onPlayerLeft, onPlayerInvited, onPartyUpdated, onPlayerPromoted,
			                  onPlayerKicked);
		}

		public async Promise Update(PartyRestriction restriction, int maxSize)
		{
			if (State == null)
			{
				return;
			}

			State = await _partyApi.UpdateParty(Id, restriction, maxSize);
			await Members.Refresh();
		}

		/// <inheritdoc cref="IPartyApi.JoinParty"/>
		public async Promise Join(string partyId)
		{
			State = await _partyApi.JoinParty(partyId);
			await Members.Refresh();
		}

		/// <inheritdoc cref="IPartyApi.LeaveParty"/>
		public async Promise Leave()
		{
			if (State == null)
			{
				return;
			}

			try
			{
				await _partyApi.LeaveParty(State.id);
			}
			finally
			{
				State = null;
			}
		}

		/// <inheritdoc cref="IPartyApi.InviteToParty"/>
		public async Promise Invite(string playerId)
		{
			if (State == null)
			{
				return;
			}

			await _partyApi.InviteToParty(State.id, playerId);
		}

		/// <inheritdoc cref="IPartyApi.PromoteToLeader"/>
		public async Promise Promote(string playerId)
		{
			if (State == null)
			{
				return;
			}

			await _partyApi.PromoteToLeader(State.id, playerId);
		}

		/// <inheritdoc cref="IPartyApi.KickPlayer"/>
		public async Promise Kick(string playerId)
		{
			if (State == null)
			{
				return;
			}

			await _partyApi.KickPlayer(State.id, playerId);
		}

		public void Dispose()
		{
			_state = null;
		}
	}
}
