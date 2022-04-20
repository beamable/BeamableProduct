using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class JoinLobbyRequest
  {
    public List<Tag> tags;

    public JoinLobbyRequest(List<Tag> tags)
    {
      this.tags = tags;
    }
  }
}
