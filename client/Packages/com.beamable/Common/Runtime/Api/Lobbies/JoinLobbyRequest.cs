using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class JoinLobbyRequest
  {
    public List<Tag> playerTags;

    public JoinLobbyRequest(List<Tag> playerTags)
    {
      this.playerTags = playerTags;
    }
  }
}
