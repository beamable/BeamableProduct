using System;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class CreatePartyRequest
	{
		/// <summary>
		/// Stringified version of the <see cref="PartyRestriction"/>
		/// </summary>
		public string restriction;

		/// <summary>
		/// Player id of a party leader.
		/// </summary>
		public string leader;

		/// <summary>
		/// Maximum allowed number of players in the party.
		/// </summary>
		public int maxSize;

		public CreatePartyRequest(string restriction, string leader, int maxSize)
		{
			this.restriction = restriction;
			this.leader = leader;
			this.maxSize = maxSize;
		}
	}
}
