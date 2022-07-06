namespace Beamable.EasyFeatures.BasicParty
{
	public class JoinPartyPlayerSystem : JoinPartyView.IDependencies
	{
		public string PartyId { get; set; }
		public bool IsVisible { get; set; }
		public bool ValidateJoinButton()
		{
			return !string.IsNullOrWhiteSpace(PartyId);
		}
	}
}
