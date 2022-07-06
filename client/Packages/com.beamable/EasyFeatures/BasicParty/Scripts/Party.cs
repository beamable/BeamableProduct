using Beamable.Common.Player;
using System;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicParty
{
	public enum PartyAccess
	{
		Private = 0,
		Public = 1,
	}
	
	[Serializable]
	public class Party : DefaultObservable, ICloneable
	{
		public string PartyId { get; set; }

		public PartyAccess Access { get; set; }

		public int MaxPlayers { get; set; }

		public List<PartySlotPresenter.ViewData> Players { get; set; }

		public Party() {}
		
		private Party(string id, PartyAccess access, int maxPlayers, List<PartySlotPresenter.ViewData> players)
		{
			PartyId = id;
			Access = access;
			MaxPlayers = maxPlayers;
			Players = players;
		}
		
		public object Clone()
		{
			return new Party(PartyId, Access, MaxPlayers, Players);
		}
	}
}
