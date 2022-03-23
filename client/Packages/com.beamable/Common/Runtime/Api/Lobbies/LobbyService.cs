using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;

namespace Beamable.Experimental.Api.Lobbies
{
  // TODO: This needs to be implemented for "Real"
  public class LobbyService : ILobbyApi
  {
    private readonly IBeamableRequester _requester;

    public LobbyService(IBeamableRequester requester)
    {
      _requester = requester;
    }

    /// <summary>
    ///   Find lobbies for the player to join.
    /// </summary>
    // TODO: This should also allow for all sorts of fun querying
    public Promise<List<Lobby>> FindLobbies()
    {
      return _requester.Request<GetLobbiesResponse>(
        Method.GET,
        $"/lobbies"
      ).Map(response => response.results);
    }

    /// <summary>
    /// Create a new lobby with the current player as the host.
    /// </summary>
    /// <param name="name">Name of the lobby</param>
    /// <param name="restriction">The privacy value for the created lobby.</param>
    /// <param name="gameTypeRef">If this lobby should be subject to matchmaking, a gametype ref should be provided</param>
    /// <param name="description">Short optional description of what the lobby is for.</param>
    public Promise<Lobby> CreateLobby(
      string name,
      LobbyRestriction restriction,
      SimGameTypeRef gameTypeRef = null,
      string description = null)
    {
      return _requester.Request<Lobby>(
        Method.POST,
        $"/lobbies",
        new CreateLobbyRequest(name, description, restriction.ToString(), gameTypeRef?.Id)
      );
    }

    /// <summary>
    /// Join a lobby given its lobby id.
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <returns></returns>
    public Promise<Lobby> JoinLobby(string lobbyId)
    {
      return _requester.Request<Lobby>(
        Method.PUT,
        $"/lobbies/{lobbyId}"
      );
    }

    public Promise<Lobby> JoinLobbyByPasscode(string passcode)
    {
      throw new System.NotImplementedException();
    }

    /// <summary>
    /// Fetch the current status of the given lobbyId
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <returns></returns>
    public Promise<Lobby> GetLobby(string lobbyId)
    {
      return _requester.Request<Lobby>(
        Method.GET,
        $"/lobbies/{lobbyId}"
      );
    }

    /// <summary>
    /// Notify the given lobby that the player intends to leave.
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <returns></returns>
    public Promise<Unit> LeaveLobby(string lobbyId)
    {
      return _requester.Request<Unit>(
        Method.DELETE,
        $"/lobbies/{lobbyId}"
      );
    }
  }
}
