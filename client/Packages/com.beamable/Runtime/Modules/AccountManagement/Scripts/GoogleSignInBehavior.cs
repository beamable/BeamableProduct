using Beamable.Common.Api.Auth;
using Beamable.Platform.SDK.Auth;
using UnityEngine;
using UnityEngine.Scripting;

namespace Beamable.AccountManagement
{
	public class GoogleSignInBehavior : MonoBehaviour
	{
		private GoogleSignIn _google;
		private ThirdPartyLoginPromise _promise;

		/// <summary>
		/// Begin the Google Sign-In process.
		/// </summary>
		/// <remarks>
		/// Wired to <c>AccountManagementSignals.ThirdPartyLoginAttempted</c> on the
		/// AccountManagementFlow prefab, which fans out to every provider behaviour; this one returns
		/// immediately unless the promise is for Google. A promise created by
		/// <see cref="AccountManagementSignals.LoginThirdPartySilently"/> is routed to
		/// <see cref="StartGoogleSilentLogin"/> from here, so the prefab needs no extra wiring.
		/// </remarks>
		/// <param name="promise">Promise to be completed when sign-in succeeds or fails</param>
		public void StartGoogleLogin(ThirdPartyLoginPromise promise)
		{
			if (promise.ThirdParty != AuthThirdParty.Google)
			{
				return;
			}

			if (promise.Silent)
			{
				StartGoogleSilentLogin(promise);
				return;
			}

			_google = CreateHarness();
			AdoptPromise(promise);

			if (Application.isEditor)
			{
				Debug.LogError("Google Sign-In is not functional in Editor. Please build to device.");
				GoogleAuthResponse("CANCELED");
				return;
			}
			_google.Login();
		}

		/// <summary>
		/// Begin a silent Google Sign-In: refresh the ID token of the account the player has already
		/// granted on this device, with no account chooser and no UI at all.
		/// </summary>
		/// <remarks>
		/// Android only, and requires <c>googlesignin-release.aar</c> 2.0.0 or newer. Everywhere else -
		/// including the Editor - the promise completes with
		/// <see cref="ThirdPartyLoginResponse.NoCredentialFound"/>, quietly, so a game can attempt this
		/// on every platform without special-casing. Public so that a project with its own
		/// AccountManagement prefab can wire it directly; the shipped prefab reaches it through
		/// <see cref="StartGoogleLogin"/> instead.
		/// </remarks>
		/// <param name="promise">Promise to be completed when the attempt resolves</param>
		public void StartGoogleSilentLogin(ThirdPartyLoginPromise promise)
		{
			if (promise.ThirdParty != AuthThirdParty.Google)
			{
				return;
			}

			_google = CreateHarness();
			AdoptPromise(promise);

			// Unlike Login(), LoginSilently() always answers exactly once - including in the Editor and
			// on platforms with no support, where it reports UNAVAILABLE rather than leaving the
			// promise hanging. So there is no isEditor special case to write here.
			_google.LoginSilently();
		}

		private GoogleSignIn CreateHarness()
		{
			return new GoogleSignIn(gameObject,
									nameof(GoogleAuthResponse),
									AccountManagementConfiguration.Instance.GoogleClientID,
									AccountManagementConfiguration.Instance.GoogleIosClientID);
		}

		/// <summary>
		/// Take ownership of a new promise, resolving any previous one first.
		/// </summary>
		/// <remarks>
		/// Starting a second attempt used to overwrite <c>_promise</c> and leave the first one
		/// unresolved forever. Because <see cref="AccountManagementSignals.LoginThirdParty"/> wraps it
		/// in <c>WithLoading</c>, that left the "Logging In..." overlay up for the rest of the session.
		/// </remarks>
		private void AdoptPromise(ThirdPartyLoginPromise promise)
		{
			var superseded = _promise;
			_promise = promise;

			if (superseded != null && !superseded.IsCompleted)
			{
				superseded.CompleteSuccess(ThirdPartyLoginResponse.Cancel());
			}
		}

		/// <summary>
		/// Callback to be invoked via UnitySendMessage when the plugin either
		/// receives a valid ID token or indicates an error.
		/// </summary>
		/// <param name="message">Response message from the Google Sign-In plugin</param>
		[Preserve]
		private void GoogleAuthResponse(string message)
		{
			var promise = _promise;
			if (promise == null)
			{
				return;
			}

			_promise = null;

			var result = GoogleSignInResult.Parse(message);

			if (result.HasIdToken)
			{
				promise.CompleteSuccess(new ThirdPartyLoginResponse(result.IdToken));
				return;
			}

			if (result.Status == GoogleSignInStatus.NoCredential)
			{
				promise.CompleteSuccess(ThirdPartyLoginResponse.NoCredentialFound());
				return;
			}

			if (result.Status == GoogleSignInStatus.Error && !promise.Silent)
			{
				promise.CompleteError(new GoogleInvalidTokenException(message));
				return;
			}

			// Cancelled, Unavailable, or a failure during a silent attempt: nothing to report to the
			// player, and nothing for the login flow to do.
			if (result.Status != GoogleSignInStatus.Cancelled)
			{
				Debug.Log($"Google Sign-In did not produce a token. {result}");
			}

			promise.CompleteSuccess(promise.Silent
										? ThirdPartyLoginResponse.NoCredentialFound()
										: ThirdPartyLoginResponse.Cancel());
		}
	}
}
