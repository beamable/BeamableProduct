using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Serialization.SmallerJSON;
using UnityEngine;

namespace Beamable.Player
{
  /// <summary>
  /// Experimental API around managing a player's lobby state.
  /// </summary>
  [Serializable]
  public class PlayerLobby : IDisposable
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

    public Lobby State
    {
      get => _state;
      private set
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

        _state = value;
      }
    }

    public bool IsInLobby => State != null;
    public string Id => SafeAccess(State?.lobbyId);
    public string Name => SafeAccess(State?.name);
    public string Description => SafeAccess(State?.description);
    public LobbyRestriction Restriction => SafeAccess(State.Restriction);
    public string Host => SafeAccess(State?.host);
    public List<LobbyPlayer> Players => SafeAccess(State?.players);
    public string Passcode => SafeAccess(State?.passcode);

    public int MaxPlayers => SafeAccess(State.maxPlayers);

    private T SafeAccess<T>(T value)
    {
      if (!IsInLobby)
      {
        throw new NotInLobby();
      }

      return value;
    }

    public Promise<LobbyQueryResponse> FindLobbies()
    {
      return _lobbyApi.FindLobbies();
    }

    public async Promise Create(
      string name,
      LobbyRestriction restriction,
      SimGameTypeRef gameTypeRef = null,
      string description = null,
      List<Tag> playerTags = null,
      List<string> statsToInclude = null)
    {
      State = await _lobbyApi.CreateLobby(name, restriction, gameTypeRef, description, playerTags, statsToInclude);
    }

    public async Promise Join(string lobbyId, List<Tag> playerTags = null)
    {
      State = await _lobbyApi.JoinLobby(lobbyId, playerTags);
    }

    public async Promise JoinByPasscode(string passcode, List<Tag> playerTags = null)
    {
      State = await _lobbyApi.JoinLobbyByPasscode(passcode, playerTags);
    }

    public async Promise AddTags(List<Tag> tags)
    {
      State = await _lobbyApi.AddPlayerTags(State.lobbyId, tags);
    }

    public async Promise RemoveTags(List<string> tags)
    {
      State = await _lobbyApi.RemovePlayerTags(State.lobbyId, tags);
    }

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

    public async Promise Refresh()
    {
      if (State == null)
      {
        return;
      }

      State = await _lobbyApi.GetLobby(State.lobbyId);
    }

    public void Dispose()
    {
      _state = null;
    }

    private async void OnRawUpdate(object message)
    {
      Debug.Log($"Received update: {message}");
      await Refresh();
    }
  }
}
