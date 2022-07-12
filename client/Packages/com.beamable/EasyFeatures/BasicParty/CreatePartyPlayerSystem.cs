using Beamable.Experimental.Api.Parties;

namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyPlayerSystem : CreatePartyView.IDependencies
	{
		public bool IsVisible { get; set; }
		public int MaxPlayers { get; set; }
		public PartyRestriction PartyRestriction { get; set; } = PartyRestriction.Unrestricted;

		public bool ValidateConfirmButton(int maxPlayers)
		{
			return maxPlayers > 0;
		}
	}
}
