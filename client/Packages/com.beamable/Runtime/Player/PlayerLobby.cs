using System;
using Beamable.Common;
using Beamable.Common.Api.Lobbies;
using Beamable.Common.Content;

namespace Player
{
  [Serializable]
  public class PlayerLobby
  {
    private readonly ILobbyApi _lobbyApi;

    public PlayerLobby(ILobbyApi lobbyApi)
    {
      _lobbyApi = lobbyApi;
    }

    public Lobby State { get; private set; }

    public bool IsInLobby => State != null;

    public async Promise Create(SimGameTypeRef gameTypeRef)
    {
      State = await _lobbyApi.CreateLobby();
    }
  }
}
