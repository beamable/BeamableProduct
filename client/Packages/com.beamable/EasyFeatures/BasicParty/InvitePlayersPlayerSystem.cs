using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicParty
{
	public class InvitePlayersPlayerSystem : InvitePlayersView.IDependencies
	{
		public bool IsVisible { get; set; }
		public string PlayerName { get; set; }
		public List<PartySlotPresenter.ViewData> Players { get; set; }
	}
}
