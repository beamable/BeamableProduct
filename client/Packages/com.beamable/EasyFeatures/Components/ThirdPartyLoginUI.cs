using System;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class ThirdPartyLoginUI : MonoBehaviour
	{
		public AuthMethodButton AuthMethodButtonPrefab;
		
		public Button AppleLoginButton;
		public Button FacebookLoginButton;
		public Button GameCenterLoginButton;
		public Button GooglePlayLoginButton;
		public Button SteamLoginButton;

		private void Start()
		{
			// enable/disable login buttons based on config
			// AppleLoginButton.gameObject.SetActive(appleLogin);
			// FacebookLoginButton.gameObject.SetActive(facebookLogin);
			// GameCenterLoginButton.gameObject.SetActive(gameCenterLogin);
			// GooglePlayLoginButton.gameObject.SetActive(googlePlayLogin);
			// SteamLoginButton.gameObject.SetActive(steamLogin);
			
			AppleLoginButton.onClick.ReplaceOrAddListener(OnAppleLogin);
			FacebookLoginButton.onClick.ReplaceOrAddListener(OnFacebookLogin);
			GameCenterLoginButton.onClick.ReplaceOrAddListener(OnGameCenterLogin);
			GooglePlayLoginButton.onClick.ReplaceOrAddListener(OnGooglePlayLogin);
			SteamLoginButton.onClick.ReplaceOrAddListener(OnSteamLogin);
		}

		private void OnSteamLogin()
		{
			throw new NotImplementedException();
		}

		private void OnGooglePlayLogin()
		{
			throw new NotImplementedException();
		}

		private void OnGameCenterLogin()
		{
			throw new NotImplementedException();
		}

		private void OnFacebookLogin()
		{
			throw new NotImplementedException();
		}

		private void OnAppleLogin()
		{
			throw new NotImplementedException();
		}
	}
}
