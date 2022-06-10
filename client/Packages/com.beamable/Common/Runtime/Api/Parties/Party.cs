using Beamable.Common.Player;
using System;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class Party : DefaultObservable
	{
		public string partyId;

		public string restriction;

		public string host;

		public int maxPlayers;
	}
}
