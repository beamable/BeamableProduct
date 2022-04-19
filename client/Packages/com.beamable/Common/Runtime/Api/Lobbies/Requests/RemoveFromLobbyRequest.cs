using System;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class RemoveFromLobbyRequest
  {
    public string playerId;

    public RemoveFromLobbyRequest(string playerId)
    {
      this.playerId = playerId;
    }
  }
}
