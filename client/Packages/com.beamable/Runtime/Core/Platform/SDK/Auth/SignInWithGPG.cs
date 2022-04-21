#if BEAMABLE_GPGS && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;

namespace Beamable.Platform.SDK.Auth
{
	public class SignInWithGPG
	{
		public Action<bool> OnLoginResult;
		public Action<bool, string> OnRequestServerSideAccessResult;

		public SignInWithGPG()
		{
			PlayGamesPlatform.Activate();
		}

		public void Login()
		{
			PlayGamesPlatform.Instance.Authenticate(HandleAuthenticate);
		}

		private void HandleAuthenticate(SignInStatus status)
		{
			if(status == SignInStatus.Success)
			{
				PlayGamesPlatform.Instance.RequestServerSideAccess(false, HandleRequestServerSideAccess);
			}
			OnLoginResult?.Invoke(status == SignInStatus.Success);
		}

		private void HandleRequestServerSideAccess(string key)
		{
			OnRequestServerSideAccessResult?.Invoke(!string.IsNullOrEmpty(key), key);
		}
	}
}
#endif
