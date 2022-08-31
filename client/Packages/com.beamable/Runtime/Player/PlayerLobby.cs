using Beamable.Common;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Content;
using Beamable.Common.Player;
using Beamable.Experimental.Api.Lobbies;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Player
{
	/// <summary>
	/// Experimental API around managing a player's lobby state.
	/// </summary>
	[Serializable]
	public class PlayerLobby : Observable<Lobby>, IDisposable
	{
		private readonly ILobbyApi _lobbyApi;
		private readonly INotificationService _notificationService;
		private Lobby _state;

		public PlayerLobby(ILobbyApi lobbyApi, INotificationService notificationService)
		{
			_lobbyApi = lobbyApi;
			_notificationService = notificationService;
		}

		private static string UpdateName(string lobbyId) => $"lobbies.update.{lobbyId}";

		public override Lobby Value
		{
			get => base.Value;
			set
			{
				if (value != null)
				{
					if (_state == null)
					{
						_notificationService.Subscribe(UpdateName(value.lobbyId), OnRawUpdate);
					}
				}
				else
				{
					if (_state != null)
					{
						_notificationService.Unsubscribe(UpdateName(_state.lobbyId), OnRawUpdate);
					}
				}

				if (_state == null)
				{
					_state = value; // no one has a reference to this yet, so its fine to just do a SET.
				}
				else
				{
					_state.Set(value); // someone may have a reference to the state, so we want to keep their pointer alive, but point to modern data.
				}

				base.Value = value;
			}
		}

		/// <summary>
		/// The current <see cref="Lobby"/> the player is in. If the player is not a lobby, then this field is null.
		/// A player can only be in one lobby at a time.
		/// </summary>
		public Lobby State
		{
			get => _state;
			private set => Value = value;
		}

		/// <summary>
		/// Checks if the player is in a lobby.
		/// </summary>
		public bool IsInLobby => State != null;

		/// <inheritdoc cref="Lobby.lobbyId"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public string Id => SafeAccess(State?.lobbyId);

		/// <inheritdoc cref="Lobby.name"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public string Name => SafeAccess(State?.name);

		/// <inheritdoc cref="Lobby.description"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public string Description => SafeAccess(State?.description);

		/// <inheritdoc cref="Lobby.Restriction"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public LobbyRestriction Restriction => SafeAccess(State.Restriction);

		/// <inheritdoc cref="Lobby.host"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public string Host => SafeAccess(State?.host);

		/// <inheritdoc cref="Lobby.players"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public List<LobbyPlayer> Players => SafeAccess(State?.players);

		/// <inheritdoc cref="Lobby.passcode"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public string Passcode => SafeAccess(State?.passcode);

		/// <inheritdoc cref="Lobby.maxPlayers"/>
		/// <para>This references the data in the <see cref="State"/> field, which is the player's current lobby.</para>
		public int MaxPlayers => SafeAccess(State.maxPlayers);

		private T SafeAccess<T>(T value)
		{
			if (!IsInLobby)
			{
				throw new NotInLobby();
			}

			return value;
		}

		/// <inheritdoc cref="ILobbyApi.FindLobbies"/>
		public Promise<LobbyQueryResponse> FindLobbies()
		{
			return _lobbyApi.FindLobbies();
		}

		/// <inheritdoc cref="ILobbyApi.CreateLobby"/>
		public async Promise Create(
		  string name,
		  LobbyRestriction restriction,
		  SimGameTypeRef gameTypeRef = null,
		  string description = null,
		  List<Tag> playerTags = null,
		  int? maxPlayers = null,
		  int? passcodeLength = null,
		  List<string> statsToInclude = null)
		{
			State = await _lobbyApi.CreateLobby(
			  name,
			  restriction,
			  gameTypeRef,
			  description,
			  playerTags,
			  maxPlayers,
			  passcodeLength,
			  statsToInclude);
		}

		/// <inheritdoc cref="ILobbyApi.JoinLobby"/>
		public async Promise Join(string lobbyId, List<Tag> playerTags = null)
		{
			State = await _lobbyApi.JoinLobby(lobbyId, playerTags);
		}

		/// <inheritdoc cref="ILobbyApi.JoinLobbyByPasscode"/>
		public async Promise JoinByPasscode(string passcode, List<Tag> playerTags = null)
		{
			State = await _lobbyApi.JoinLobbyByPasscode(passcode, playerTags);
		}

		/// <inheritdoc cref="ILobbyApi.AddPlayerTags"/>
		public async Promise AddTags(List<Tag> tags, bool replace = false)
		{
			State = await _lobbyApi.AddPlayerTags(State.lobbyId, tags, replace: replace);
		}

		/// <inheritdoc cref="ILobbyApi.RemovePlayerTags"/>
		public async Promise RemoveTags(List<string> tags)
		{
			State = await _lobbyApi.RemovePlayerTags(State.lobbyId, tags);
		}

		/// <summary>
		/// Leave the lobby if the player is in a lobby.
		/// </summary>
		public async Promise Leave()
		{
			if (State == null)
			{
				return;
			}

			try
			{
				await _lobbyApi.LeaveLobby(State.lobbyId);
			}
			finally
			{
				State = null;
			}
		}

		protected override async Promise PerformRefresh()
		{
			if (State == null) return; // nothing to do.

			State = await _lobbyApi.GetLobby(State.lobbyId);
		}

		public void Dispose()
		{
			_state = null;
		}

		private void OnRawUpdate(object message)
		{
			var _ = Refresh(); // fire and forget- go update.
		}
	}
}
