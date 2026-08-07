using Beamable.Common;
using Beamable.Platform.SDK.Auth;

namespace Beamable.AccountManagement
{
	/// <summary>
	/// Convenience wrappers around <see cref="GoogleSignInService"/> that read the Google OAuth
	/// client IDs out of <see cref="AccountManagementConfiguration"/>, so a game does not have to
	/// pass them at every call site.
	/// </summary>
	/// <remarks>
	/// This lives in the AccountManagement module - assembly <c>Unity.Beamable</c> - on purpose.
	/// <see cref="AccountManagementConfiguration"/> is an obsolete configuration object and lives
	/// here, while <see cref="GoogleSignInService"/> lives in the lower-level
	/// <c>Beamable.Platform</c> assembly, which must not depend upwards on it. A game that does not
	/// want the obsolete configuration should call <see cref="GoogleSignInService"/> directly with
	/// its own client IDs.
	/// </remarks>
	public static class GoogleSignInConfigHelper
	{
		/// <summary>The configured Google OAuth web client ID. This is the one Android uses.</summary>
		public static string WebClientId => AccountManagementConfiguration.Instance.GoogleClientID;

		/// <summary>The configured Google OAuth iOS client ID.</summary>
		public static string IosClientId => AccountManagementConfiguration.Instance.GoogleIosClientID;

		/// <inheritdoc cref="GoogleSignInService.SignInSilently"/>
		public static Promise<GoogleSignInResult> SignInSilently(
			float timeoutSeconds = GoogleSignInService.DEFAULT_SILENT_TIMEOUT_SECONDS)
			=> GoogleSignInService.SignInSilently(WebClientId, timeoutSeconds);

		/// <inheritdoc cref="GoogleSignInService.SignIn"/>
		public static Promise<GoogleSignInResult> SignIn()
			=> GoogleSignInService.SignIn(WebClientId, IosClientId);

		/// <inheritdoc cref="GoogleSignInService.SignOut"/>
		public static Promise<GoogleSignInResult> SignOut()
			=> GoogleSignInService.SignOut(WebClientId);

		/// <inheritdoc cref="GoogleSignInService.RevokeAccess"/>
		public static Promise<GoogleSignInResult> RevokeAccess()
			=> GoogleSignInService.RevokeAccess(WebClientId);
	}
}
