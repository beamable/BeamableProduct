using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class CreateLobbyRequest
  {
    public string name;
    public string description;
    public string restriction;
    public string matchType;
    public List<Tag> playerTags;

    public CreateLobbyRequest(
      string name,
      string description,
      string restriction,
      string matchType,
      List<Tag> playerTags)
    {
      this.name = name;
      this.description = description;
      this.restriction = restriction;
      this.matchType = matchType;
      this.playerTags = playerTags;
    }
  }
}
