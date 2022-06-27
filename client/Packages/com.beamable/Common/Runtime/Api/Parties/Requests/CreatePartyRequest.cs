using System;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class CreatePartyRequest
	{
		/// <summary>
		/// Stringified version of the <see cref="PartyAccess"/>
		/// </summary>
		public string access;

		public int? maxPlayers;

		public CreatePartyRequest(string access, int? maxPlayers)
		{
			this.access = access;
			this.maxPlayers = maxPlayers;
		}
	}
}
