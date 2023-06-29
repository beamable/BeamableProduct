#if BEAMABLE_GPGS && UNITY_ANDROID
using Beamable.Common;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;

namespace Beamable.Platform.SDK.Auth
{

	/// <summary>
	/// SignInWithGPG is a helper class.
	/// It provides Google Play Games auth methods by taking advantage of Beam Promises system.
	/// </summary>
	public class SignInWithGPG
	{
        /// <summary>
        /// Event for handling local login result.
        /// It will be called only when using non static Login method.
        /// </summary>
		public Action<bool> OnLoginResult;

        /// <summary>
        /// Event for handling server side token access result. 
        /// It will be called only when using non static Login method.
        /// </summary>
		public Action<bool, string> OnRequestServerSideAccessResult;

        /// <summary>
        /// Property that controls if call for server side access will try to get token that could be useful for long lived access.
        /// </summary>
		public static bool ForceRefreshToken { get; set; } = true;

		public SignInWithGPG()
		{
			PlayGamesPlatform.Activate();
		}

        /// <summary>
        /// Performs login first, then it will request server side token.
        /// </summary>
		public void Login()
		{
			PerformLocalLogin().Then(HandleAuthenticate);
		}

        /// <summary>
        /// Performs login to Google Play Games Services.
        /// It does not handle server side access.
        /// </summary>
        /// <returns>Promise with SignInStatus value</returns>
		public static Promise<SignInStatus> PerformLocalLogin()
		{
			var promise = new Promise<SignInStatus>();
			if (PlayGamesPlatform.Instance.IsAuthenticated())
			{
				promise.CompleteSuccess(SignInStatus.Success);
			}
			else
			{
				PlayGamesPlatform.Instance.Authenticate(status =>
				{
					if (status == SignInStatus.Success)
					{
						promise.CompleteSuccess(SignInStatus.Success);
					}
					else
					{
						promise.CompleteError(new Exception("Local login failed"));
					}
				});
			}
        
			return promise;
		}


        /// <summary>
        /// Performs request to Google Play Games Services for server side access token.
        /// </summary>
        /// <returns>Promise with string value containing the server side token</returns>
		public static Promise<string> RequestServerSideToken()
		{
			var promise = new Promise<string>();
			PlayGamesPlatform.Instance.RequestServerSideAccess(ForceRefreshToken, result =>
			{
				if(string.IsNullOrEmpty(result))
					promise.CompleteError(new Exception("Cannot get server side token"));
				else
					promise.CompleteSuccess(result);
			});
			return promise;
		}

		private void HandleAuthenticate(SignInStatus status)
		{
			OnLoginResult?.Invoke(status == SignInStatus.Success);
			if(status == SignInStatus.Success)
			{
				RequestServerSideToken().Then(HandleRequestServerSideAccess);
			}
		}

		private void HandleRequestServerSideAccess(string key)
		{
			OnRequestServerSideAccessResult?.Invoke(!string.IsNullOrEmpty(key), key);
		}
	}
}
#endif
