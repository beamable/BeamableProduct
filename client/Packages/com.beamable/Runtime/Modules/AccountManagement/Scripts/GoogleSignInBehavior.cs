using Beamable.Common.Api.Auth;
using Beamable.Platform.SDK.Auth;
using UnityEngine;

namespace Beamable.AccountManagement
{
	/// <summary>
	/// Adapts the AccountManagementFlow prefab's third party login signal onto
	/// <see cref="GoogleSignInService"/>.
	/// </summary>
	/// <remarks>
	/// <para>This behaviour deliberately hosts no native callback of its own. The Google plugins answer
	/// through <c>UnitySendMessage(objectName, methodName, message)</c>, which carries no correlation
	/// id, so a behaviour that owned the callback could only ever have one channel: a silent attempt
	/// started at launch and an interactive attempt started by a button press would both answer to the
	/// same method, and whichever arrived first would settle whichever promise the behaviour happened
	/// to be holding - potentially handing the login flow the <i>previously</i> signed-in account's ID
	/// token. <see cref="GoogleSignInService"/> creates a uniquely named
	/// <see cref="GoogleSignInReceiver"/> per request, which is the only correlation channel that
	/// exists, and applies <see cref="GoogleSignInService.DEFAULT_SILENT_TIMEOUT_SECONDS"/> to the
	/// silent path so a dropped <c>UnitySendMessage</c> cannot leave a promise - and with it the
	/// "Logging In..." overlay - pending forever.</para>
	///
	/// <para>The public method signatures are fixed by the prefab's <c>ThirdPartyLoginAttempted</c>
	/// wiring, so they are kept exactly as they were.</para>
	/// </remarks>
	public class GoogleSignInBehavior : MonoBehaviour
	{
		/// <summary>
		/// The request this behaviour is waiting on, if any. At most one at a time: an attempt that
		/// arrives while this one is unresolved is refused rather than allowed to replace it.
		/// </summary>
		private ThirdPartyLoginPromise _pending;

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
			if (promise == null || promise.ThirdParty != AuthThirdParty.Google)
			{
				return;
			}

			if (promise.Silent)
			{
				StartGoogleSilentLogin(promise);
				return;
			}

			// The account chooser can only be open once, and the request that opened it is the one whose
			// response will arrive. So a second press is refused rather than allowed to supersede the
			// first: superseding would discard the token the player is in the middle of granting.
			if (IsPending())
			{
				Debug.Log("A Google Sign-In attempt is already in progress; ignoring this one.");
				promise.CompleteSuccess(ThirdPartyLoginResponse.Cancel());
				return;
			}

			if (!GoogleSignInService.IsSignInSupported)
			{
				// Not fatal - the request below completes as a cancellation - but worth saying out loud,
				// because a Google button that does nothing in the Editor is otherwise a mystery.
				Debug.LogError("Google Sign-In is not functional in Editor. Please build to device.");
			}

			_pending = promise;

			GoogleSignInConfigHelper.SignIn().Then(result => Settle(promise, result));
		}

		/// <summary>
		/// Begin a silent Google Sign-In: refresh the ID token of the account the player has already
		/// granted on this device, with no account chooser and no UI at all.
		/// </summary>
		/// <remarks>
		/// Android only, and requires <c>googlesignin-release.aar</c>
		/// <see cref="GoogleSignInService.MINIMUM_SILENT_PLUGIN_VERSION"/> or newer. Everywhere else -
		/// including the Editor - the promise completes with
		/// <see cref="ThirdPartyLoginResponse.NoCredentialFound"/>, quietly, so a game can attempt this
		/// on every platform without special-casing. Public so that a project with its own
		/// AccountManagement prefab can wire it directly; the shipped prefab reaches it through
		/// <see cref="StartGoogleLogin"/> instead.
		/// </remarks>
		/// <param name="promise">Promise to be completed when the attempt resolves</param>
		public void StartGoogleSilentLogin(ThirdPartyLoginPromise promise)
		{
			if (promise == null || promise.ThirdParty != AuthThirdParty.Google)
			{
				return;
			}

			// Refusing an overlap is not just a policy here: GoogleSignInService coalesces concurrent
			// silent requests onto one native call, so a second promise would otherwise resolve to the
			// first request's result rather than to a request of its own.
			if (IsPending())
			{
				Debug.Log("A Google Sign-In attempt is already in progress; ignoring this silent one.");
				promise.CompleteSuccess(ThirdPartyLoginResponse.NoCredentialFound());
				return;
			}

			_pending = promise;

			GoogleSignInConfigHelper.SignInSilently().Then(result => Settle(promise, result));
		}

		/// <summary>
		/// Apply a request's result to the promise that request was started for.
		/// </summary>
		/// <remarks>
		/// The promise is captured per request rather than read out of a field, so a response that
		/// arrives late - after the behaviour has moved on, or after
		/// <see cref="GoogleSignInService"/> has timed the request out - can only ever reach its own
		/// promise. That, plus the already-resolved guard in
		/// <see cref="GoogleSignInResponseMapping.Apply"/>, is what stops one attempt's ID token from
		/// completing another attempt.
		/// </remarks>
		private void Settle(ThirdPartyLoginPromise promise, GoogleSignInResult result)
		{
			if (ReferenceEquals(_pending, promise))
			{
				_pending = null;
			}

			GoogleSignInResponseMapping.Apply(promise, result);
		}

		private bool IsPending() => _pending != null && !_pending.IsCompleted;

		/// <summary>
		/// Settle anything still in flight when this behaviour goes away.
		/// </summary>
		/// <remarks>
		/// The native request is not cancelled - the chooser may still own the screen, and its response
		/// lands on a <see cref="GoogleSignInReceiver"/> that survives scene loads and will simply find
		/// a settled promise. What must not survive is an <i>unsettled</i> promise:
		/// <see cref="AccountManagementSignals.LoginThirdParty"/> wraps it in <c>WithLoading</c>, and
		/// the loading indicator plays sessions strictly in order, so a single promise that never
		/// completes wedges every later loading overlay in the game.
		/// </remarks>
		private void OnDestroy()
		{
			var pending = _pending;
			_pending = null;

			if (pending == null || pending.IsCompleted)
			{
				return;
			}

			pending.CompleteSuccess(pending.Silent
										? ThirdPartyLoginResponse.NoCredentialFound()
										: ThirdPartyLoginResponse.Cancel());
		}
	}

	/// <summary>
	/// Translates a <see cref="GoogleSignInResult"/> into the login flow's vocabulary.
	/// </summary>
	/// <remarks>
	/// Deliberately separate from <see cref="GoogleSignInBehavior"/> and free of any MonoBehaviour
	/// state, so the whole outcome matrix - which is where the account-switching consequences live -
	/// is covered by plain unit tests instead of only on a device.
	/// </remarks>
	public static class GoogleSignInResponseMapping
	{
		/// <summary>
		/// Settle <paramref name="promise"/> with the login flow's reading of <paramref name="result"/>.
		/// </summary>
		/// <remarks>
		/// A no-op on an already-resolved promise. That is the guarantee the overlap rules rest on: a
		/// refused attempt is settled at the moment it is refused, and a native response that arrives
		/// afterwards - possibly carrying a <i>different</i> Google account's ID token - cannot
		/// overwrite it.
		/// </remarks>
		public static void Apply(ThirdPartyLoginPromise promise, GoogleSignInResult result)
		{
			if (promise == null)
			{
				return;
			}

			if (promise.IsCompleted)
			{
				Debug.Log($"Discarding a Google Sign-In result for an attempt that has already been " +
						  $"resolved. {result}");
				return;
			}

			if (result.HasIdToken)
			{
				promise.CompleteSuccess(new ThirdPartyLoginResponse(result.IdToken));
				return;
			}

			// A failed silent attempt is not the player's problem: it is reported as "no credential" so
			// the flow offers the Google button instead of raising an error nobody asked for.
			if (!promise.Silent && result.Status == GoogleSignInStatus.Error)
			{
				promise.CompleteError(new GoogleInvalidTokenException(Describe(result)));
				return;
			}

			if (!result.IsBenign)
			{
				// Cancelled, NoCredential and Unavailable are ordinary outcomes, and GoogleSignInService
				// already logs the reason for Unavailable. What is left - a silent attempt that errored,
				// or a Busy - deserves a breadcrumb, because nothing else will mention it.
				Debug.Log($"Google Sign-In did not produce a token. {result}");
			}

			promise.CompleteSuccess(ToNoTokenResponse(result, promise.Silent));
		}

		/// <summary>
		/// The benign response for a result that carries no ID token.
		/// </summary>
		/// <remarks>
		/// <see cref="GoogleSignInStatus.NoCredential"/> is reported as such whether or not the attempt
		/// was silent, because it is actionable either way: nobody has granted an account on this
		/// device. Everything else that reaches here during an interactive attempt - Cancelled,
		/// Unavailable, Busy - is an ordinary cancellation.
		/// </remarks>
		public static ThirdPartyLoginResponse ToNoTokenResponse(GoogleSignInResult result, bool silent)
		{
			if (silent || result.Status == GoogleSignInStatus.NoCredential)
			{
				return ThirdPartyLoginResponse.NoCredentialFound();
			}

			return ThirdPartyLoginResponse.Cancel();
		}

		/// <summary>
		/// The message for a <see cref="GoogleInvalidTokenException"/>. Never contains an ID token.
		/// </summary>
		public static string Describe(GoogleSignInResult result) =>
			string.IsNullOrEmpty(result.Detail) ? result.ToString() : result.Detail;
	}
}
