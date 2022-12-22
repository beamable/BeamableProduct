using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class SwitchAccountPopup : MonoBehaviour
	{
		public Button SignInButton;
		public Button CreateAccountButton;

		public void Setup(UnityAction signInAction, UnityAction createAccountAction)
		{
			SignInButton.onClick.AddListener(signInAction);
			CreateAccountButton.onClick.AddListener(createAccountAction);
			gameObject.SetActive(true);
		}
	}
}
