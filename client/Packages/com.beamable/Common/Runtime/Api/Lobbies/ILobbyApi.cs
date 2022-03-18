using Beamable.Common;

namespace Beamable.Experimental.Api.Lobbies
{
  public interface ILobbyApi
  {
    Promise<Lobby> CreateLobby(string name);
    Promise<Lobby> JoinLobby();
  }
}
