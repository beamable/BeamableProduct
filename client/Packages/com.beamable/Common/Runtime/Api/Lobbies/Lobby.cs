using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class Lobby
  {
    public string lobbyId;
    public string name;
    public string description;
    public string restriction;
    public string host;
    public List<LobbyPlayer> players;
    public string passcode;

    public LobbyRestriction Restriction => (LobbyRestriction)Enum.Parse(typeof(LobbyRestriction), restriction);
  }
}
