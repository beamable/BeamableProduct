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
    private readonly IUserContext _userContext;

    public LobbyService(IBeamableRequester requester, IUserContext userContext)
    {
      _requester = requester;
      _userContext = userContext;
    }

    /// <summary>
    ///   Find lobbies for the player to join.
    /// </summary>
    // TODO: This should also allow for all sorts of fun querying
    public Promise<LobbyQueryResponse> FindLobbies()
    {
      return _requester.Request<LobbyQueryResponse>(
        Method.GET,
        $"/lobbies"
      );
    }

    /// <summary>
    /// Create a new lobby with the current player as the host.
    /// </summary>
    /// <param name="name">Name of the lobby</param>
    /// <param name="restriction">The privacy value for the created lobby.</param>
    /// <param name="gameTypeRef">If this lobby should be subject to matchmaking, a gametype ref should be provided</param>
    /// <param name="description">Short optional description of what the lobby is for.</param>
    /// <param name="playerTags"></param>
    /// <param name="statsToInclude"></param>
    public Promise<Lobby> CreateLobby(
      string name,
      LobbyRestriction restriction,
      SimGameTypeRef gameTypeRef = null,
      string description = null,
      List<Tag> playerTags = null,
      List<string> statsToInclude = null)
    {
      return _requester.Request<Lobby>(
        Method.POST,
        $"/lobbies",
        new CreateLobbyRequest(name, description, restriction.ToString(), gameTypeRef?.Id, playerTags)
      );
    }

    /// <summary>
    /// Join a lobby given its lobby id.
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <param name="playerTags"></param>
    public Promise<Lobby> JoinLobby(string lobbyId, List<Tag> playerTags = null)
    {
      return _requester.Request<Lobby>(
        Method.PUT,
        $"/lobbies/{lobbyId}",
        new JoinLobbyRequest(playerTags)
      );
    }

    public Promise<Lobby> JoinLobbyByPasscode(string passcode, List<Tag> playerTags = null)
    {
      return _requester.Request<Lobby>(
        Method.PUT,
        $"/lobbies/passcode",
        new JoinByPasscodeRequest(passcode, playerTags)
      );
    }

    /// <summary>
    /// Fetch the current status of the given lobbyId
    /// </summary>
    /// <param name="lobbyId"></param>
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
    public Promise<Unit> LeaveLobby(string lobbyId)
    {
      return _requester.Request<Unit>(
        Method.DELETE,
        $"/lobbies/{lobbyId}",
        new RemoveFromLobbyRequest(_userContext.UserId.ToString())
      );
    }

    /// <summary>
    /// Add a list of tags to the given player in the given lobby.
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <param name="tags"></param>
    /// <param name="playerId"></param>
    public Promise<Lobby> AddPlayerTags(string lobbyId, List<Tag> tags, string playerId = null)
    {
      playerId ??= _userContext.UserId.ToString();
      return _requester.Request<Lobby>(
        Method.PUT,
        $"/lobbies/{lobbyId}/tags",
        new AddTagsRequest(playerId, tags)
      );
    }

    /// <summary>
    /// Remove a list of tags from the given player in the given lobby.
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <param name="tags"></param>
    /// <param name="playerId"></param>
    public Promise<Lobby> RemovePlayerTags(string lobbyId, List<string> tags, string playerId = null)
    {
      playerId ??= _userContext.UserId.ToString();
      return _requester.Request<Lobby>(
        Method.PUT,
        $"/lobbies/{lobbyId}/tags",
        new RemoveTagsRequest(playerId, tags)
      );
    }

    /// <summary>
    /// Send a request to the given lobby to remove the player with the given playerId. If the
    /// requesting player doesn't have the capability to boot players, this will throw an exception.
    /// </summary>
    /// <param name="lobbyId">The lobby to remove the player from</param>
    /// <param name="playerId">The player to remove</param>
    public Promise<Unit> BootPlayer(string lobbyId, string playerId)
    {
      return _requester.Request<Unit>(
        Method.DELETE,
        $"/lobbies/{lobbyId}",
        new RemoveFromLobbyRequest(playerId)
      );
    }
  }
}
