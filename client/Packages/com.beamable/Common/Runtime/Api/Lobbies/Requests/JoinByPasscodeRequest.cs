using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class JoinByPasscodeRequest
  {
    public string passcode;
    public List<Tag> playerTags;

    public JoinByPasscodeRequest(string passcode, List<Tag> playerTags)
    {
      this.passcode = passcode;
      this.playerTags = playerTags;
    }
  }
}
