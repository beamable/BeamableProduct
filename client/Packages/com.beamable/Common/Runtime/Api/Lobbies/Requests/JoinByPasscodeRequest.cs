using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
  [Serializable]
  public class JoinByPasscodeRequest
  {
    public string passcode;
    public List<Tag> tags;

    public JoinByPasscodeRequest(string passcode, List<Tag> tags)
    {
      this.passcode = passcode;
      this.tags = tags;
    }
  }
}
