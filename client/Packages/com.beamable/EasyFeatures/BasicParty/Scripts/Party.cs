using Beamable.Common.Player;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicParty
{
	public enum PartyAccess
	{
		Private,
		Public,
	}
	
	public class Party : DefaultObservable
	{
		public string PartyId { get; set; }

		public PartyAccess Access { get; set; }

		public int MaxPlayers { get; set; }

		public List<PartySlotPresenter.ViewData> Players { get; set; }
	}
}
