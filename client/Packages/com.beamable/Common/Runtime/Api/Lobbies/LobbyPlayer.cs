using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class LobbyPlayer
  {
    public string playerId;
    /// <summary>
    /// List of optional tags associated with the player. This can be used to generate teams, other
    /// arbitrary groupings of players per the creator's needs.
    /// </summary>
    public List<Tag> tags;
    /// <summary>
    /// Populated by the stats requested upon lobby creation.
    /// </summary>
    public Dictionary<string, string> stats;
    public DateTime joined;
  }

  [Serializable]
  public class Tag
  {
    public string name;
    public string value;

    public Tag(string name, string value)
    {
      this.name = name;
      this.value = value;
    }
  }
}
