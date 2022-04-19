using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class AddTagsRequest
  {
    public string playerId;
    public List<Tag> tags;

    public AddTagsRequest(string playerId, List<Tag> tags)
    {
      this.playerId = playerId;
      this.tags = tags;
    }
  }
}
