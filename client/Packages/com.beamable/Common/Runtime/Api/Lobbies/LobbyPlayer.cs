using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class LobbyPlayer
  {
    public string playerId;
    public List<Tag> tags;
  }

  [Serializable]
  public class Tag
  {
    public string name;
    public string value;
  }
}
