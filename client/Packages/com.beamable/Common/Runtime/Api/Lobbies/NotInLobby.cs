// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

using System;

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Exception thrown when making requests to PlayerLobby when a player is not in
	/// a <see cref="Lobby"/>.
	/// </summary>
	public class NotInLobby : Exception { }
}
