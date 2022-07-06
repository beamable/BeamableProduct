using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyPlayerSystem : BasicPartyView.IDependencies
	{
		public List<PartySlotPresenter.ViewData> SlotsData => BuildViewData();
		public bool IsVisible { get; set; }
		public Party Party { get; set; }
		public bool IsPlayerLeader { get; set; }

		private List<PartySlotPresenter.ViewData> _players;
		
		public void Setup(List<PartySlotPresenter.ViewData> players)
		{
			_players = players;
		}
		
		private List<PartySlotPresenter.ViewData> BuildViewData()
		{
			List<PartySlotPresenter.ViewData> data = new List<PartySlotPresenter.ViewData>(_players);
			return data;
		}
	}
}
