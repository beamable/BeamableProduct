// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

using System;

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Exception thrown when making requests to <see cref="Beamable.Player.PlayerLobby"/> when a player is not in
	/// a <see cref="Lobby"/>.
	/// </summary>
	public class NotInLobby : Exception { }
}
