using Beamable.Common.Content;
using System;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class UpdatePartyRequest
	{
		/// <summary>
		/// Stringified version of the <see cref="PartyRestriction"/>
		/// </summary>
		public string restriction;

		/// <summary>
		/// Maximum allowed number of players in the party.
		/// </summary>
		public Optional<int> maxSize;

		public UpdatePartyRequest(string restriction, int maxSize)
		{
			this.restriction = restriction;
			this.maxSize = maxSize;
		}

		public UpdatePartyRequest(string restriction)
		{
			this.restriction = restriction;
		}
	}
}
