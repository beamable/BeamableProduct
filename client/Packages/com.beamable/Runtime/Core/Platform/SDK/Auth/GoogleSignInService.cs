using Beamable.Common;
using System;
using UnityEngine;

namespace Beamable.Platform.SDK.Auth
{
	/// <summary>
	/// Promise-based Google Sign-In that needs no scene object, no prefab, and no
	/// <c>AccountManagementFlow</c>: it creates and destroys its own hidden
	/// <see cref="GoogleSignInReceiver"/> per request. Safe to call from any script at any time,
	/// including during startup.
	/// </summary>
	/// <remarks>
	/// <para><b>Silent sign-in</b> (<see cref="SignInSilently"/>) is Android only, and requires
	/// <c>googlesignin-release.aar</c> 2.0.0 or newer. Everywhere else - iOS, the Editor, standalone,
	/// WebGL - it reports <see cref="GoogleSignInStatus.Unavailable"/> and logs at
	/// <see cref="Debug.Log"/> level, so calling it unconditionally at startup on every platform is
	/// safe and quiet.</para>
	///
	/// <para><b>These promises do not fail.</b> Every outcome, including errors, arrives as a
	/// successful promise carrying a <see cref="GoogleSignInResult"/>. That keeps a routine silent
	/// attempt from tripping Beamable's uncaught-promise handler, and means callers can
	/// <c>await</c> without a try/catch. Inspect <see cref="GoogleSignInResult.Status"/>.</para>
	///
	/// <para>Typical startup use - restore the player's Google-linked Beamable account with no UI:
	/// <code>
	/// var google = await GoogleSignInService.SignInSilently(webClientId);
	/// if (!google.IsSuccess) { return; } // NoCredential: show the Google button instead
	///
	/// var recovery = await BeamContext.Default.Accounts
	///     .RecoverAccountWithThirdParty(AuthThirdParty.Google, google.IdToken);
	/// if (recovery.isSuccess) { await recovery.SwitchToAccount(); }
	/// </code>
	/// </para>
	/// </remarks>
	public static class GoogleSignInService
	{
		/// <summary>
		/// How long a silent request waits before giving up, in seconds of running-player time.
		/// </summary>
		/// <remarks>
		/// A timeout is not optional here. <c>UnitySendMessage</c> silently drops the response if the
		/// player's native libraries are not loaded - it only logs "Native libraries not loaded -
		/// dropping message" - in which case the request would never complete <i>and never fail</i>.
		/// Silent sign-in is especially exposed to this because the natural place to call it is during
		/// startup. The value is generous because a local token refresh takes well under a second,
		/// and a spurious abandonment is worse than waiting.
		/// </remarks>
		public const float DEFAULT_SILENT_TIMEOUT_SECONDS = 30f;

		/// <summary>The first plugin version that has silent sign-in.</summary>
		public const string MINIMUM_SILENT_PLUGIN_VERSION = "2.0.0";

		private const string LOG_PREFIX = "[Beamable] Google Sign-In: ";
		private const string JAVA_CLASS_NAME = "com.beamable.googlesignin.GoogleSignInActivity";

		private static int _requestCount;
		private static Promise<GoogleSignInResult> _silentRequest;
		private static Promise<GoogleSignInResult> _interactiveRequest;

		// Platform-guarded because every Beamable assembly compiles with -warnaserror+, and off
		// Android this field is written but never read, which is CS0414.
#if UNITY_ANDROID && !UNITY_EDITOR
		private static bool? _isSilentSignInSupported;
#endif

		/// <summary>
		/// Whether interactive Google Sign-In can run in this build at all: an Android or iOS player.
		/// False in the Editor, where the native plugins do not exist.
		/// </summary>
		public static bool IsSignInSupported
		{
			get
			{
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// Whether <see cref="SignInSilently"/> can run in this build: an Android player with a
		/// <c>googlesignin-release.aar</c> of at least <see cref="MINIMUM_SILENT_PLUGIN_VERSION"/>.
		/// </summary>
		/// <remarks>
		/// The first read costs one JNI round trip and is then cached for the lifetime of the
		/// process. Reading this property early - off the startup critical path - is a way to pay that
		/// cost when it does not matter.
		/// </remarks>
		public static bool IsSilentSignInSupported
		{
			get
			{
#if UNITY_ANDROID && !UNITY_EDITOR
				if (_isSilentSignInSupported.HasValue)
				{
					return _isSilentSignInSupported.Value;
				}

				try
				{
					using (var plugin = new AndroidJavaClass(JAVA_CLASS_NAME))
					{
						// getPluginVersion() was added by the same plugin release as silentLogin(), so
						// its presence is the feature check. An older .aar throws instead of answering.
						var version = plugin.CallStatic<string>("getPluginVersion");
						_isSilentSignInSupported = !string.IsNullOrEmpty(version);
					}
				}
				catch (Exception e)
				{
					// Either an .aar that predates silent sign-in, or one stripped by R8 because the
					// project has no keep rule for com.beamable.googlesignin.**.
					Debug.Log($"{LOG_PREFIX}silent sign-in is unavailable. Update " +
							  $"Packages/com.beamable/Plugins/Android/googlesignin-release.aar to " +
							  $"{MINIMUM_SILENT_PLUGIN_VERSION} or newer. ({e.Message})");
					_isSilentSignInSupported = false;
				}

				return _isSilentSignInSupported.Value;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// Refresh the Google ID token of the account the player has already granted on this device,
		/// with no account chooser and no UI of any kind.
		/// </summary>
		/// <remarks>
		/// On a device where nobody has signed in yet, or where consent is required, this reports
		/// <see cref="GoogleSignInStatus.NoCredential"/> - the expected outcome, not an error. Fall
		/// back to <see cref="SignIn"/> from a button press.
		///
		/// <para>Concurrent silent requests are coalesced: calling this while one is in flight returns
		/// the same promise rather than starting a second attempt.</para>
		/// </remarks>
		/// <param name="webClientId">
		/// The Google OAuth <b>web</b> client ID. Must be the same value used for
		/// <see cref="SignIn"/>, or the cached credential will not be recognised.
		/// </param>
		/// <param name="timeoutSeconds">
		/// Give up after this many seconds of running-player time. 0 or less disables the timeout,
		/// which risks a promise that never settles - see
		/// <see cref="DEFAULT_SILENT_TIMEOUT_SECONDS"/>.
		/// </param>
		public static Promise<GoogleSignInResult> SignInSilently(
			string webClientId,
			float timeoutSeconds = DEFAULT_SILENT_TIMEOUT_SECONDS)
		{
			if (_silentRequest != null && !_silentRequest.IsCompleted)
			{
				return _silentRequest;
			}

			if (!Application.isPlaying)
			{
				return Unavailable("silent sign-in requires play mode.");
			}

			if (!IsSilentSignInSupported)
			{
				return Unavailable(IsSignInSupported
					? $"silent sign-in needs googlesignin-release.aar {MINIMUM_SILENT_PLUGIN_VERSION} or newer."
					: $"silent sign-in is only supported on Android players (platform={Application.platform}).");
			}

			if (string.IsNullOrEmpty(webClientId))
			{
				return Unavailable("no Google web client ID was provided.");
			}

			_silentRequest = AndroidRequest("silentLogin", webClientId, timeoutSeconds);
			return _silentRequest;
		}

		/// <summary>
		/// Show the Google account chooser and sign in interactively.
		/// </summary>
		/// <remarks>
		/// No timeout is applied: the chooser can legitimately own the screen for minutes, and
		/// abandoning a request the player is still working through would be worse than waiting. A
		/// second call while one is in flight reports <see cref="GoogleSignInStatus.Busy"/> and does
		/// nothing.
		/// </remarks>
		/// <param name="webClientId">Google OAuth web client ID, used on Android.</param>
		/// <param name="iosClientId">Google OAuth iOS client ID, used on iOS.</param>
		public static Promise<GoogleSignInResult> SignIn(string webClientId, string iosClientId)
		{
			if (_interactiveRequest != null && !_interactiveRequest.IsCompleted)
			{
				return Promise<GoogleSignInResult>.Successful(
					GoogleSignInResult.Busy("An interactive Google Sign-In request is already in flight."));
			}

			if (!Application.isPlaying)
			{
				return Unavailable("Google Sign-In requires play mode.");
			}

			if (!IsSignInSupported)
			{
				return Unavailable($"Google Sign-In is only supported on Android and iOS players " +
								   $"(platform={Application.platform}).");
			}

#if UNITY_IOS && !UNITY_EDITOR
			var clientId = iosClientId;
			var clientIdName = "iOS";
#else
			var clientId = webClientId;
			var clientIdName = "web";
#endif
			if (string.IsNullOrEmpty(clientId))
			{
				return Unavailable($"no Google {clientIdName} client ID was provided.");
			}

			var promise = new Promise<GoogleSignInResult>();
			_interactiveRequest = promise;

			var receiver = CreateReceiver(promise, timeoutSeconds: 0f);

			try
			{
				// Reuse the existing low-level harness, which owns the per-platform dispatch. The
				// receiver's GameObject name is what it passes to the native side.
				new GoogleSignIn(receiver.gameObject, GoogleSignInReceiver.RESPONSE_METHOD, webClientId, iosClientId)
					.Login();
			}
			catch (Exception e)
			{
				FailRequest(promise, receiver, "could not start Google Sign-In", e);
			}

			return promise;
		}

		/// <summary>
		/// Forget the cached Google account, so the next <see cref="SignInSilently"/> reports
		/// <see cref="GoogleSignInStatus.NoCredential"/>.
		/// </summary>
		/// <remarks>
		/// This is what a game's "log out" should call. Without it, a player who logs out of Beamable
		/// is silently signed back into the same Google account on the next launch and cannot hand the
		/// device to someone else. It does not withdraw the OAuth grant, so signing back in does not
		/// re-prompt for consent. Android only.
		/// </remarks>
		public static Promise<GoogleSignInResult> SignOut(string webClientId)
		{
			return SignOutRequest("signOut", webClientId);
		}

		/// <summary>
		/// Withdraw the Google OAuth grant entirely.
		/// </summary>
		/// <remarks>
		/// <b>Destructive:</b> the player must pass through the full consent screen again next time.
		/// Wire this to "delete account" or "unlink Google" only - use <see cref="SignOut"/> for an
		/// ordinary log out. Android only.
		/// </remarks>
		public static Promise<GoogleSignInResult> RevokeAccess(string webClientId)
		{
			return SignOutRequest("revokeAccess", webClientId);
		}

		private static Promise<GoogleSignInResult> SignOutRequest(string javaMethod, string webClientId)
		{
			if (!Application.isPlaying)
			{
				return Unavailable($"{javaMethod} requires play mode.");
			}

			if (!IsSilentSignInSupported)
			{
				return Unavailable($"{javaMethod} is only supported on Android players with " +
								   $"googlesignin-release.aar {MINIMUM_SILENT_PLUGIN_VERSION} or newer.");
			}

			if (string.IsNullOrEmpty(webClientId))
			{
				return Unavailable("no Google web client ID was provided.");
			}

			return AndroidRequest(javaMethod, webClientId, DEFAULT_SILENT_TIMEOUT_SECONDS);
		}

		/// <summary>
		/// Dispatch one of the plugin's static, String-only entry points and bridge its
		/// <c>UnitySendMessage</c> answer onto a promise.
		/// </summary>
		private static Promise<GoogleSignInResult> AndroidRequest(
			string javaMethod,
			string webClientId,
			float timeoutSeconds)
		{
			var promise = new Promise<GoogleSignInResult>();

#if UNITY_ANDROID && !UNITY_EDITOR
			var receiver = CreateReceiver(promise, timeoutSeconds);

			try
			{
				using (var plugin = new AndroidJavaClass(JAVA_CLASS_NAME))
				{
					plugin.CallStatic(javaMethod, receiver.gameObject.name, GoogleSignInReceiver.RESPONSE_METHOD,
									  webClientId);
				}
			}
			catch (Exception e)
			{
				FailRequest(promise, receiver, $"could not call {javaMethod}", e);
			}
#else
			// Unreachable: every caller checks IsSilentSignInSupported first, which is false off
			// Android. Kept so the file compiles on every target without the JNI types.
			promise.CompleteSuccess(GoogleSignInResult.Unavailable(
				$"{javaMethod} is only supported on Android players."));
#endif

			return promise;
		}

		private static GoogleSignInReceiver CreateReceiver(Promise<GoogleSignInResult> promise, float timeoutSeconds)
		{
			// The name is the correlation channel for UnitySendMessage, so it has to be unique. The
			// counter keeps it readable in logs; the GUID fragment guarantees it cannot collide with a
			// receiver left over from an earlier session or with a GameObject the game happens to own.
			var objectName = $"BeamableGoogleSignIn-{++_requestCount}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

			return GoogleSignInReceiver.Create(
				objectName,
				message => promise.CompleteSuccess(GoogleSignInResult.Parse(message)),
				timeoutSeconds);
		}

		private static void FailRequest(
			Promise<GoogleSignInResult> promise,
			GoogleSignInReceiver receiver,
			string what,
			Exception exception)
		{
			Debug.Log($"{LOG_PREFIX}{what}. {exception.Message}");

			// Complete first: CompleteSuccess is idempotent, so the receiver's OnDestroy - which also
			// tries to settle - becomes a no-op and this more specific message wins.
			promise.CompleteSuccess(GoogleSignInResult.Unavailable($"{what}: {exception.Message}"));

			if (receiver != null)
			{
				UnityEngine.Object.Destroy(receiver.gameObject);
			}
		}

		private static Promise<GoogleSignInResult> Unavailable(string reason)
		{
			Debug.Log($"{LOG_PREFIX}{reason}");
			return Promise<GoogleSignInResult>.Successful(GoogleSignInResult.Unavailable(reason));
		}

		/// <summary>
		/// Clear per-session state. Required for "Enter Play Mode Options" with domain reload
		/// disabled, where statics survive leaving play mode: a request abandoned mid-session would
		/// otherwise leave <see cref="SignIn"/> reporting <see cref="GoogleSignInStatus.Busy"/>
		/// forever. Mirrors the same pattern in <c>BeamContext</c>.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			_requestCount = 0;
			_silentRequest = null;
			_interactiveRequest = null;
#if UNITY_ANDROID && !UNITY_EDITOR
			_isSilentSignInSupported = null;
#endif
		}
	}
}
