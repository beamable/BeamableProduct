using System;

namespace Beamable.Experimental.Api.Parties
{
	/// <summary>
	/// Exception thrown when making requests to PlayerLobby when a player is not in a <see cref="Party"/>.
	/// </summary>
	public class NotInParty : Exception { }
}
