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

		public CreatePartyRequest(string restriction, string leader)
		{
			this.restriction = restriction;
			this.leader = leader;
		}
	}
}
