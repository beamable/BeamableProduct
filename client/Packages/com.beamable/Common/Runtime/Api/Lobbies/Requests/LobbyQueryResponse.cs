// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

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
