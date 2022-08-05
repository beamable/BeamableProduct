using System;

namespace Beamable.Experimental.Api.Lobbies
{
	[Serializable]
	public class LobbyQueryRequest
	{
		// TODO: add description
		public int skip;

		// TODO: add description
		public int limit;

		// TODO: add description
		public string matchType;

		public LobbyQueryRequest(int skip, int limit, string matchType)
		{
			this.skip = skip;
			this.limit = limit;
			this.matchType = matchType;
		}
	}
}
