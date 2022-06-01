using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyPlayerSystem : BasicPartyView.IDependencies
	{
		public List<PartySlotPresenter.ViewData> SlotsData => BuildViewData();
		public bool IsVisible { get; set; }
		public PartyData PartyData { get; set; }
		public bool IsPlayerLeader { get; set; }
		
		

		public void Setup(List<PartySlotPresenter.ViewData> players)
		{
			
		}
		
		private List<PartySlotPresenter.ViewData> BuildViewData()
		{
			List<PartySlotPresenter.ViewData> data = new List<PartySlotPresenter.ViewData>(PartyData.MaxPlayers);

			for (int i = 0; i < PartyData.MaxPlayers; i++)
			{
				PartySlotPresenter.ViewData entry = new PartySlotPresenter.ViewData();

				if (i < PlayerIds.Count)
				{
					entry.PlayerId = PlayerIds[i];
					entry.IsReady = PlayerReadiness[i];
				}
				else
				{
					entry.PlayerId = string.Empty;
					entry.IsReady = false;
				}

				data.Add(entry);
			}

			return data;
		}
	}
}
