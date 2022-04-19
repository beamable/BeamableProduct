using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Content;

namespace Beamable.Experimental.Api.Lobbies
{
  public interface ILobbyApi
  {
    Promise<LobbyQueryResponse> FindLobbies();
    Promise<Lobby> CreateLobby(
      string name,
      LobbyRestriction restriction,
      SimGameTypeRef gameTypeRef = null,
      string description = null,
      List<Tag> playerTags = null,
      List<string> statsToInclude = null);
    Promise<Lobby> JoinLobby(string lobbyId, List<Tag> playerTags = null);
    Promise<Lobby> JoinLobbyByPasscode(string passcode, List<Tag> playerTags = null);
    Promise<Lobby> GetLobby(string lobbyId);
    Promise<Unit> LeaveLobby(string lobbyId);
    Promise<Lobby> AddPlayerTags(string lobbyId, List<Tag> tags, string playerId = null);
    Promise<Lobby> RemovePlayerTags(string lobbyId, List<string> tags, string playerId = null);
    Promise<Unit> BootPlayer(string lobbyId, string playerId);
  }
}
