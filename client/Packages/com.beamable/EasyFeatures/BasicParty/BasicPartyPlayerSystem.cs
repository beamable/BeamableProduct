namespace Beamable.EasyFeatures.BasicParty
{
	public class BasicPartyPlayerSystem : BasicPartyView.IDependencies
	{
		public bool IsVisible { get; set; }
		public PartyData PartyData { get; }
		public bool IsPlayerLeader { get; }
	}
}
