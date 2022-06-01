namespace Beamable.EasyFeatures.BasicParty
{
	public enum PartyAccess
	{
		Private,
		Public,
	}
	
	public class PartyData
	{
		public string PartyId { get; set; }

		public PartyAccess Access { get; set; }

		public int MaxPlayers { get; set; }
	}
}
