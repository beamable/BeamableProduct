using Beamable.Avatars;
using Beamable.Experimental.Api.Parties;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyPlayerSystem : CreatePartyView.IDependencies, JoinPartyView.IDependencies
	{
		public int MaxPlayers { get; set; }
		public PartyRestriction PartyRestriction { get; set; }
		public string PartyIdToJoin { get; set; }

		public bool ValidateJoinButton()
		{
			return !string.IsNullOrWhiteSpace(PartyIdToJoin);
		}

		public bool ValidateConfirmButton(int maxPlayers)
		{
			return maxPlayers > 0;
		}
	}
}
