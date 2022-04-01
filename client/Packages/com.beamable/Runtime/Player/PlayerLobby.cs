using System;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Lobbies;

namespace Beamable.Player
{
  /// <summary>
  /// Experimental API around managing a player's lobby state.
  /// </summary>
  [Serializable]
  public class PlayerLobby
  {
    private readonly ILobbyApi _lobbyApi;

    public PlayerLobby(ILobbyApi lobbyApi)
    {
      _lobbyApi = lobbyApi;
    }

    public Lobby State { get; private set; }

    public bool IsInLobby => State != null;

    public async Promise Create(
      string name,
      LobbyRestriction restriction,
      SimGameTypeRef gameTypeRef = null,
      string description = null)
    {
      State = await _lobbyApi.CreateLobby(name, restriction, gameTypeRef, description);
      // TODO: Need to subscribe to lobby messages
    }

    public async Promise Join(string lobbyId)
    {
      State = await _lobbyApi.JoinLobby(lobbyId);
      // TODO: Need to subscribe to lobby messages
    }

    // public async Promise JoinByPasscode(string passcode)
    // {
    //   State = await _lobbyApi.JoinLobbyByPasscode(passcode);
    //   // TODO: Need to subscribe to lobby messages
    // }

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
        // TODO: Need to unsubscribe from lobby messages
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
  }
}
