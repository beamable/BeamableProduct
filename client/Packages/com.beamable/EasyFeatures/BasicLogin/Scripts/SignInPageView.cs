using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLogin.Scripts
{
	public class SignInPageView : MonoBehaviour, ISyncBeamableView
	{
		[Header("View Configuration")]
		public int EnrichOrder;

		public Button EmailButton;
		public Button AppleButton;
		public Button FacebookButton;
		public Button FacebookLimitedButton;
		public Button GoogleButton;
		public Button GooglePlayGamesButton;
		public Button SteamButton;
		public Button GameCenterButton;
		public Button GameCenterLimitedButton;
		public Button NewAccountButton;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var viewDeps = currentContext.ServiceProvider.GetService<ILoginDeps>();

			EmailButton.gameObject.SetActive(!viewDeps.CurrentUser.HasEmail);
			FacebookButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.Facebook().ShouldShowButton);
			AppleButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.Apple().ShouldShowButton);
			GoogleButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.Google().ShouldShowButton);
			GooglePlayGamesButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.GooglePlayGameServices().ShouldShowButton);
			SteamButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.Steam().ShouldShowButton);
			GameCenterButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.GameCenter().ShouldShowButton);
			GameCenterLimitedButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.GameCenterLimited().ShouldShowButton);
			FacebookLimitedButton.gameObject.SetActive(viewDeps.CurrentUser.thirdParties.FacebookLimited().ShouldShowButton);

			// TODO: What to do about Limited facebook and gamecenter? Are those ever possible to enable at the same time?
		}
	}
}
