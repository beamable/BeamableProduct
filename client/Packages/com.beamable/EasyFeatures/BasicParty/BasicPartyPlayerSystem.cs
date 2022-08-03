using Beamable.Avatars;
using Beamable.Experimental.Api.Parties;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyPlayerSystem : BasicPartyView.IDependencies
	{
		public List<PartySlotPresenter.ViewData> SlotsData => BuildViewData();
		public bool IsVisible { get; set; }
		public Party Party { get; set; }
		public int MaxPlayers { get; set; }

		private List<string> _players;
		
		public void Setup(List<string> players, int maxPlayers)
		{
			_players = players;
			MaxPlayers = maxPlayers;
		}
		
		private List<PartySlotPresenter.ViewData> BuildViewData()
		{
			PartySlotPresenter.ViewData[] data = new PartySlotPresenter.ViewData[_players.Count];
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = new PartySlotPresenter.ViewData
				{
					Avatar = AvatarConfiguration.Instance.Default.Sprite, IsReady = false, PlayerId = _players[i]
				};
			}
			
			return data.ToList();
		}
	}
}
