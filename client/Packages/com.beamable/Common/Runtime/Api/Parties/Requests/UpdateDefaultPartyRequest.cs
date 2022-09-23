using System;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class UpdateDefaultPartyRequest
	{
		/// <summary>
		/// Stringified version of the <see cref="PartyRestriction"/>
		/// </summary>
		public string restriction;

		public UpdateDefaultPartyRequest(string restriction)
		{
			this.restriction = restriction;
		}
	}
}
