using System;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class CreateLobbyRequest
  {
    public string name;
    public string description;
    public string restriction;
    public string matchType;

    public CreateLobbyRequest(
      string name,
      string description,
      string restriction,
      string matchType)
    {
      this.name = name;
      this.description = description;
      this.restriction = restriction;
      this.matchType = matchType;
    }
  }
}
