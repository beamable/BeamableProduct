// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Value used to determine behavior around who is able to join a <see cref="Lobby"/>.
	/// </summary>
	public enum LobbyRestriction
	{
		/// <summary>
		/// Open lobbies allow any player to join as well as show up in queries for lobbies.
		/// </summary>
		Open,
		/// <summary>
		/// Closed lobbies are hidden from lobby queries and require knowledge of a passcode to join.
		/// </summary>
		Closed
	}
}
