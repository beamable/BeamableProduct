using Beamable.Common;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Player;
using Beamable.Experimental.Api.Parties;
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
		private Party _state;
		private Action<object> _onPlayerJoined;
		private Action<object> _onPlayerLeft;

		public PlayerParty(IPartyApi partyApi, INotificationService notificationService)
		{
			_partyApi = partyApi;
			_notificationService = notificationService;
			Members = new ObservableReadonlyList<string>(RefreshMembersList);
		}

		private static string PlayersLeftName(string partyId) => $"party.players_left.{partyId}";
		private static string PlayersJoinedName(string partyId) => $"party.players_joined.{partyId}";

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
					}
				}
				else
				{
					if (_state != null)
					{
						_notificationService.Unsubscribe(PlayersLeftName(_state.id), PlayerLeft);
						_notificationService.Unsubscribe(PlayersJoinedName(_state.id), PlayerJoined);
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

		private async void PlayerJoined(object playerId)
		{
			await Refresh();
			_onPlayerJoined?.Invoke(playerId);
		}

		private async void PlayerLeft(object playerId)
		{
			await Refresh();
			_onPlayerLeft?.Invoke(playerId);
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
		public bool IsInParty => State != null;

		/// <inheritdoc cref="Party.id"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public string Id => SafeAccess(State?.id);

		/// <inheritdoc cref="Party.Restriction"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public PartyRestriction Restriction => SafeAccess(State.Restriction);

		/// <inheritdoc cref="Party.leader"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public string Leader => SafeAccess(State?.leader);

		/// <inheritdoc cref="Party.members"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current party.</para>
		public ObservableReadonlyList<string> Members { get; private set; }

		private T SafeAccess<T>(T value)
		{
			if (!IsInParty)
			{
				throw new NotInParty();
			}

			return value;
		}

		/// <inheritdoc cref="IPartyApi.CreateParty"/>
		public async Promise Create(PartyRestriction restriction, Action<object> onPlayerJoined = null, Action<object> onPlayerLeft = null)
		{
			State = await _partyApi.CreateParty(restriction);
			await Members.Refresh();
			_onPlayerJoined = onPlayerJoined;
			_onPlayerLeft = onPlayerLeft;
		}

		/// <inheritdoc cref="IPartyApi.JoinParty"/>
		public async Promise Join(string partyId)
		{
			State = await _partyApi.JoinParty(partyId);
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
		
		/// <inheritdoc cref="IPartyApi.InviteToParty"/>
		public async Promise Invite(long playerId) => await Invite(playerId.ToString());

		/// <inheritdoc cref="IPartyApi.PromoteToLeader"/>
		public async Promise Promote(string playerId)
		{
			if (State == null)
			{
				return;
			}

			await _partyApi.PromoteToLeader(State.id, playerId);
		}
		
		/// <inheritdoc cref="IPartyApi.PromoteToLeader"/>
		public async Promise Promote(long playerId) => await Promote(playerId.ToString());

		public void Dispose()
		{
			_state = null;
		}
	}
}
