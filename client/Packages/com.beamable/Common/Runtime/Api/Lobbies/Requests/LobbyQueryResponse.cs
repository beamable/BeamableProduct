// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Response payload including a list of <see cref="Lobby"/>.
	/// </summary>
	[Serializable]
	public class LobbyQueryResponse
	{
		/// <summary>
		/// List of <see cref="Lobby"/> representing the results of the query.
		/// </summary>
		public List<Lobby> results;
	}
}
