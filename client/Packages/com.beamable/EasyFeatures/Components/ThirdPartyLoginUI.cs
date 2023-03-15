using Beamable.AccountManagement;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Beamable.EasyFeatures.Components
{
	public class ThirdPartyLoginUI : MonoBehaviour
	{
		/// <summary>
		/// Subscribe to this event to handle third party authentication.
		/// </summary>
		public static event Action<ThirdPartyLoginPromise> OnThirdPartyLoginStarted;
		
		public AuthMethodButton AuthMethodButtonPrefab;

		private BeamContext _context;

		public void Setup(BeamContext context, UnityAction onEmailLoginPressed = null)
		{
			foreach (Transform child in transform)
			{
				Destroy(child.gameObject);
			}
			
			_context = context;

			if (onEmailLoginPressed != null)
			{
				Instantiate(AuthMethodButtonPrefab, transform).SetupEmail(true, onEmailLoginPressed);	
			}
			
			if (AccountManagementConfiguration.Instance.Apple)
				Instantiate(AuthMethodButtonPrefab, transform).SetupThirdParty(AuthThirdParty.Apple, true, OnAppleLogin);
			if (AccountManagementConfiguration.Instance.Facebook)
				Instantiate(AuthMethodButtonPrefab, transform).SetupThirdParty(AuthThirdParty.Facebook, true, OnFacebookLogin);
			if (AccountManagementConfiguration.Instance.Google)
				Instantiate(AuthMethodButtonPrefab, transform).SetupThirdParty(AuthThirdParty.Google, true, OnGooglePlayLogin);
		}
		
		private void OnSteamLogin()
		{
			InitThirdPartyLogin(AuthThirdParty.Steam);
		}

		private void OnGooglePlayLogin()
		{
			InitThirdPartyLogin(AuthThirdParty.Google);
		}

		private void OnGameCenterLogin()
		{
			InitThirdPartyLogin(AuthThirdParty.GameCenter);
		}

		private void OnFacebookLogin()
		{
#if BEAMABLE_FACEBOOK
			InitThirdPartyLogin(AuthThirdParty.Facebook);
#else
			Debug.LogError("Facebook is not configured properly");
#endif
		}

		private void OnAppleLogin()
		{
			InitThirdPartyLogin(AuthThirdParty.Apple);
		}

		private void InitThirdPartyLogin(AuthThirdParty thirdParty)
		{
			var promise = new ThirdPartyLoginPromise(thirdParty);
			
			OnThirdPartyLoginStarted?.Invoke(promise);
			
			promise.Then(response =>
			{
				var _ = HandleToken(thirdParty, response);
			});
		}

		private async Promise HandleToken(AuthThirdParty thirdParty, ThirdPartyLoginResponse response)
		{
			if (_context.Accounts.Current.HasThirdParty(thirdParty))
			{
				Debug.LogError($"User {_context.PlayerId} is already associated with {thirdParty}");
			}
			else
			{
				var result = await _context.Accounts.Current.AddThirdParty(thirdParty, response.AuthToken);
				
				if (result.isSuccess)
				{
					Debug.Log($"{thirdParty} token has been added to user {_context.PlayerId}");	
				}
				else
				{
					Debug.LogError($"Error: {result.error} - {result.innerException}");
				}
			}
		}
	}
}
