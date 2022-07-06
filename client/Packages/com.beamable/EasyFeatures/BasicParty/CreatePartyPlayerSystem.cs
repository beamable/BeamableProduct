namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyPlayerSystem : CreatePartyView.IDependencies
	{
		public Party Party { get; set; }
		public bool IsVisible { get; set; }
		public bool ValidateConfirmButton(int maxPlayers)
		{
			return maxPlayers > 0;
		}
	}
}
