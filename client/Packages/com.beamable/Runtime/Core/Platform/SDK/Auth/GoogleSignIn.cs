using System;
using UnityEngine;

namespace Beamable.Platform.SDK.Auth
{
	public class GoogleSignIn
	{
		private const string JAVA_CLASS_NAME = "com.beamable.googlesignin.GoogleSignInActivity";

		private readonly GameObject _target;
		private readonly string _callbackMethod;
		private readonly string _webClientId;
		private readonly string _iosClientId;

		/// <summary>
		/// Google Sign-In harness. Because the Android plugin needs to use
		/// UnitySendMessage to call back, we need to know the GameObject and
		/// callback method name.
		/// </summary>
		/// <remarks>
		/// For new code prefer <see cref="GoogleSignInService"/>, which hosts its own callback
		/// receiver and returns a <see cref="Beamable.Common.Promise{T}"/> instead of requiring a
		/// GameObject with a magically-named method.
		/// </remarks>
		/// <param name="target">GameObject to use for callback</param>
		/// <param name="callbackMethod">Name of the method to call back</param>
		/// <param name="webClientId">Google OAuth client ID - web ID for login on Android devices</param>
		/// <param name="iosClientId">Google OAuth client ID - iOS ID for login on Apple devices</param>
		public GoogleSignIn(GameObject target, string callbackMethod, string webClientId, string iosClientId)
		{
			_target = target;
			_callbackMethod = callbackMethod;
			_webClientId = webClientId;
			_iosClientId = iosClientId;
		}

		/// <summary>
		/// Initiate login using the Android native plugin. When complete, the
		/// plugin will call back to the GameObject specified in the constructor.
		/// </summary>
		public void Login()
		{
#if UNITY_ANDROID
         if (string.IsNullOrEmpty(_webClientId))
         {
            Debug.LogError("Please configure Google Client ID in the AccountManagementConfiguration asset.");
            return;
         }
         var login = new AndroidJavaClass(JAVA_CLASS_NAME);
         login.CallStatic("login", _target.name, _callbackMethod, _webClientId);
#elif UNITY_IOS
         if (string.IsNullOrEmpty(_iosClientId))
         {
            Debug.LogError("Please configure Google Client ID in the AccountManagementConfiguration asset.");
            return;
         }
         GoogleSignIn_Login(_iosClientId, _target.name, _callbackMethod);
#else
			Debug.LogError($"Google Sign-In unavailable. clientId={_webClientId}, platform={Application.platform}");
#endif // UNITY_ANDROID || UNITY_IOS
		}

		/// <summary>
		/// Initiate a silent login: refresh the ID token of the Google account the player has already
		/// granted on this device, with no account chooser and no UI. Android only.
		/// </summary>
		/// <remarks>
		/// <para>Unlike <see cref="Login"/>, this always calls back exactly once - including on
		/// platforms where it cannot run, where it answers with an UNAVAILABLE response rather than
		/// leaving the caller waiting. Callers should route the message through
		/// <see cref="GoogleSignInResult.Parse"/> so they can tell
		/// <see cref="GoogleSignInStatus.NoCredential"/> (no credential yet; show a sign-in button)
		/// apart from a real failure.</para>
		///
		/// <para>Requires <c>googlesignin-release.aar</c> 2.0.0 or newer; an older plugin has no
		/// <c>silentLogin</c> method and is reported as unavailable.</para>
		/// </remarks>
		public void LoginSilently()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (string.IsNullOrEmpty(_webClientId))
			{
				SendUnavailable("no Google web client ID is configured.");
				return;
			}

			try
			{
				using (var plugin = new AndroidJavaClass(JAVA_CLASS_NAME))
				{
					plugin.CallStatic("silentLogin", _target.name, _callbackMethod, _webClientId);
				}
			}
			catch (Exception e)
			{
				// Most likely a googlesignin-release.aar that predates silent sign-in, or one whose
				// entry points were stripped by R8.
				SendUnavailable($"could not call silentLogin - update googlesignin-release.aar to 2.0.0 " +
								$"or newer. ({e.Message})");
			}
#else
			SendUnavailable($"silent Google Sign-In is only supported on Android players " +
							$"(platform={Application.platform}).");
#endif // UNITY_ANDROID && !UNITY_EDITOR
		}

		/// <summary>
		/// Deliver a synthetic response to the same callback the native plugin would have used, so
		/// that every code path answers exactly once. GameObject.SendMessage reaches private methods,
		/// which is what UnitySendMessage does natively.
		/// </summary>
		private void SendUnavailable(string reason)
		{
			Debug.Log($"[Beamable] Google Sign-In: {reason}");

			if (_target == null)
			{
				return;
			}

			_target.SendMessage(_callbackMethod,
								$"{GoogleSignInResult.SENTINEL_UNAVAILABLE} - {reason}",
								SendMessageOptions.DontRequireReceiver);
		}

#if UNITY_IOS
      [System.Runtime.InteropServices.DllImport("__Internal")]
      private static extern void GoogleSignIn_Login(string clientId, string callbackObject, string callbackMethod);
#endif // UNITY_IOS

		/// <summary>
		/// Unpack the response from the Google Sign-In plugin. Call this from
		/// the GameObject callback.
		/// </summary>
		/// <remarks>
		/// Kept for backwards compatibility, and now a thin shim over
		/// <see cref="GoogleSignInResult.Parse"/>. This callback shape cannot express the difference
		/// between "the player cancelled", "there is no cached credential" and "Google Sign-In is
		/// unavailable here" - they all arrive as <c>callback(null)</c>. Use
		/// <see cref="GoogleSignInResult.Parse"/> directly, or
		/// <see cref="GoogleSignInService"/>, when that distinction matters.
		/// </remarks>
		/// <param name="message">Response message from the plugin</param>
		/// <param name="callback">Callback to be invoked when the result is complete</param>
		/// <param name="errback">Callback to call if authentication failed</param>
		public static void HandleResponse(string message, Action<string> callback, Action<GoogleInvalidTokenException> errback)
		{
			var result = GoogleSignInResult.Parse(message);

			if (result.HasIdToken)
			{
				callback.Invoke(result.IdToken);
			}
			else if (result.Status == GoogleSignInStatus.Error)
			{
				errback.Invoke(new GoogleInvalidTokenException(message));
			}
			else
			{
				// Cancelled, NoCredential and Unavailable all mean "no token, but nothing went
				// wrong", which this signature can only express as a null token.
				callback.Invoke(null);
			}
		}
	}

	public class GoogleInvalidTokenException : Exception
	{
		public GoogleInvalidTokenException(string message) : base(message) { }
	}
}
