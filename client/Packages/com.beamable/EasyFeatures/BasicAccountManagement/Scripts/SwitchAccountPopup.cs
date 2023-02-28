using Beamable.EasyFeatures.Components;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class SwitchAccountPopup : SlidingPopup
	{
		public Button SignInButton;
		public Button CreateAccountButton;

		public void Setup(UnityAction signInAction, UnityAction createAccountAction, OverlaysController overlaysController)
		{
			base.Setup(overlaysController);
			
			SignInButton.onClick.AddListener(signInAction);
			CreateAccountButton.onClick.AddListener(createAccountAction);
			gameObject.SetActive(true);
		}
	}
}
