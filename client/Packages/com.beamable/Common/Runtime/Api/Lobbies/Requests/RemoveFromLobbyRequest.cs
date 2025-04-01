// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC3
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC3

using System;

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Request payload to be used whenever removing a player from the <see cref="Lobby"/>. This includes leaving the
	/// <see cref="Lobby"/> as well as kicking a player out of the <see cref="Lobby"/>.
	/// </summary>
	[Serializable]
	public class RemoveFromLobbyRequest
	{
		/// <summary>
		/// The id of the player to remove from the <see cref="Lobby"/>.
		/// </summary>
		public string playerId;

		public RemoveFromLobbyRequest(string playerId)
		{
			this.playerId = playerId;
		}
	}
}
