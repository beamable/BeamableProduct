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
