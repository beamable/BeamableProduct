using Beamable.EasyFeatures.BasicParty;

namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyPlayerSystem : CreatePartyView.IDependencies
	{
		public bool IsVisible { get; set; }
		public string PartyId { get; set; }
		public int MaxPlayers { get; set; }
		public bool ValidateConfirmButton()
		{
			return MaxPlayers > 0;
		}
	}
}
