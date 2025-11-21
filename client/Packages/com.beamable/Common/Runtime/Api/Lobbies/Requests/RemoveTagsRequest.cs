// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Request payload to remove <see cref="Lobby"/> <see cref="Tag"/>s from the requested player.
	/// </summary>
	[Serializable]
	public class RemoveTagsRequest
	{
		/// <summary>
		/// The player id.
		/// </summary>
		public string playerId;

		/// <summary>
		/// List of "names" of tags to remove from the given player.
		/// </summary>
		public List<string> tags;

		public RemoveTagsRequest(string playerId, List<string> tags)
		{
			this.playerId = playerId;
			this.tags = tags;
		}
	}
}
