using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicParty
{
	public class InvitePlayersPlayerSystem : InvitePlayersView.IDependencies
	{
		public Party Party { get; set; }
		public bool IsVisible { get; set; }
		public List<PartySlotPresenter.ViewData> FriendsList { get; set; }
	}
}
