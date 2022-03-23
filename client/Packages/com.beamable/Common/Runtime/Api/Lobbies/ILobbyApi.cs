using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Content;

namespace Beamable.Experimental.Api.Lobbies
{
  public interface ILobbyApi
  {
    Promise<List<Lobby>> FindLobbies();
    Promise<Lobby> CreateLobby(
      string name,
      LobbyRestriction restriction,
      SimGameTypeRef gameTypeRef = null,
      string description = null);
    Promise<Lobby> JoinLobby(string lobbyId);
    // Promise<Lobby> JoinLobbyByPasscode(string passcode);
    Promise<Lobby> GetLobby(string lobbyId);
    Promise<Unit> LeaveLobby(string lobbyId);
  }
}
