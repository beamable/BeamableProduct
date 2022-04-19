using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class RemoveTagsRequest
  {
    public string playerId;
    public List<string> tags;

    public RemoveTagsRequest(string playerId, List<string> tags)
    {
      this.playerId = playerId;
      this.tags = tags;
    }
  }
}
