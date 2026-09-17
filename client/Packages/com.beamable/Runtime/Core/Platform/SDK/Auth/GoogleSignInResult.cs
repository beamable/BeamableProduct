using System;

namespace Beamable.Platform.SDK.Auth
{
	/// <summary>
	/// The outcome of a Google Sign-In request.
	/// </summary>
	/// <remarks>
	/// <see cref="Error"/> is deliberately the zero value, so that a default-constructed
	/// <see cref="GoogleSignInResult"/> fails closed rather than claiming success.
	/// </remarks>
	public enum GoogleSignInStatus
	{
		/// <summary>Something went wrong. <see cref="GoogleSignInResult.Detail"/> says what.</summary>
		Error = 0,

		/// <summary>An ID token was obtained. See <see cref="GoogleSignInResult.IdToken"/>.</summary>
		Success,

		/// <summary>The player dismissed the account chooser. Not an error.</summary>
		Cancelled,

		/// <summary>
		/// A silent attempt found no usable credential on this device, or consent is required.
		/// The expected outcome on a fresh install, and not an error: show a Google button instead.
		/// </summary>
		NoCredential,

		/// <summary>
		/// Google Sign-In cannot service the request in this build at all - the Editor, a platform
		/// with no Google Sign-In support, or a bundled googlesignin-release.aar that predates the
		/// requested feature. Not an error in the player's world; a reason to hide the option.
		/// </summary>
		Unavailable,

		/// <summary>An interactive request is already in flight. The new request did nothing.</summary>
		Busy
	}

	/// <summary>
	/// The result of a Google Sign-In request, and the parser for the native plugins' flat-string
	/// response protocol.
	/// </summary>
	/// <remarks>
	/// This type exists because the older <see cref="GoogleSignIn.HandleResponse"/> callback shape
	/// collapsed distinct outcomes together: a caller could not tell "the player cancelled" from
	/// "there is no cached credential" from "the plugin failed", which is exactly the distinction a
	/// silent sign-in attempt needs in order to decide whether to fall back to showing a button.
	///
	/// <para><see cref="Parse"/> is intentionally free of any UnityEngine dependency, so the whole
	/// response vocabulary is covered by plain unit tests.</para>
	/// </remarks>
	public struct GoogleSignInResult
	{
		/// <summary>
		/// Sentinels emitted by the native plugins. These are a wire contract shared with
		/// plugins/google-signin (Android) and Plugins/iOS/BeamableGoogleSignIn (iOS); do not reword
		/// them without changing both sides.
		/// </summary>
		private const string SENTINEL_CANCELED = "CANCELED";
		private const string SENTINEL_UNKNOWN = "UNKNOWN";
		private const string SENTINEL_EXCEPTION = "EXCEPTION";
		private const string SENTINEL_NO_CREDENTIAL = "NO_CREDENTIAL";
		private const string SENTINEL_SIGNED_OUT = "SIGNED_OUT";
		private const string SENTINEL_REVOKED = "REVOKED";

		/// <summary>
		/// Accepted as an alias for <see cref="SENTINEL_NO_CREDENTIAL"/>. It is the name of the
		/// underlying Google status code (CommonStatusCodes.SIGN_IN_REQUIRED), so accepting it costs
		/// nothing and means a rename on the Java side cannot break this SDK.
		/// </summary>
		private const string SENTINEL_SIGN_IN_REQUIRED = "SIGN_IN_REQUIRED";

		/// <summary>
		/// Never emitted by a native plugin - produced on the C# side when a request cannot be
		/// dispatched at all. Recognised here so that every "cannot do this" path funnels through
		/// one place.
		/// </summary>
		internal const string SENTINEL_UNAVAILABLE = "UNAVAILABLE";

		/// <summary>What happened.</summary>
		public GoogleSignInStatus Status { get; private set; }

		/// <summary>
		/// The Google ID token, set only when <see cref="Status"/> is
		/// <see cref="GoogleSignInStatus.Success"/>. Pass it to Beamable as
		/// <see cref="Beamable.Common.Api.Auth.AuthThirdParty.Google"/>.
		/// </summary>
		public string IdToken { get; private set; }

		/// <summary>
		/// Human-readable detail for logging: the reason for a failure, or the extra text the plugin
		/// attached to a sentinel. Never the ID token, so this is safe to log.
		/// </summary>
		public string Detail { get; private set; }

		/// <summary>
		/// True when the request did what it was asked to. For a sign-in that means an ID token was
		/// obtained; for <see cref="GoogleSignInService.SignOut"/> and
		/// <see cref="GoogleSignInService.RevokeAccess"/> it means the operation completed, with no
		/// token involved - see <see cref="HasIdToken"/>.
		/// </summary>
		public bool IsSuccess => Status == GoogleSignInStatus.Success;

		/// <summary>True when <see cref="IdToken"/> is set.</summary>
		public bool HasIdToken => !string.IsNullOrEmpty(IdToken);

		/// <summary>
		/// True when the request completed without a token but also without anything going wrong -
		/// the player cancelled, there was no credential, or the feature is unavailable here. Use
		/// this to decide whether something is worth reporting to the player.
		/// </summary>
		public bool IsBenign => Status == GoogleSignInStatus.Cancelled ||
								Status == GoogleSignInStatus.NoCredential ||
								Status == GoogleSignInStatus.Unavailable;

		public override string ToString() => string.IsNullOrEmpty(Detail)
			? $"GoogleSignInResult({Status})"
			: $"GoogleSignInResult({Status}: {Detail})";

		public static GoogleSignInResult Success(string idToken) =>
			new GoogleSignInResult { Status = GoogleSignInStatus.Success, IdToken = idToken };

		public static GoogleSignInResult Cancelled(string detail = null) =>
			new GoogleSignInResult { Status = GoogleSignInStatus.Cancelled, Detail = detail };

		public static GoogleSignInResult NoCredential(string detail = null) =>
			new GoogleSignInResult { Status = GoogleSignInStatus.NoCredential, Detail = detail };

		public static GoogleSignInResult Unavailable(string detail) =>
			new GoogleSignInResult { Status = GoogleSignInStatus.Unavailable, Detail = detail };

		public static GoogleSignInResult Busy(string detail) =>
			new GoogleSignInResult { Status = GoogleSignInStatus.Busy, Detail = detail };

		public static GoogleSignInResult Error(string detail) =>
			new GoogleSignInResult { Status = GoogleSignInStatus.Error, Detail = detail };

		/// <summary>
		/// Turn a native plugin response into a result.
		/// </summary>
		/// <remarks>
		/// Anything that is not a recognised sentinel is treated as an ID token, which is how the
		/// protocol has always worked. That is safe because a Google ID token is a base64url JWT
		/// whose header always begins with the lowercase "eyJ", while every sentinel begins with an
		/// uppercase letter - and because a sentinel only matches when it is followed by end of
		/// string, a space, or a dash, so a token that merely started with sentinel-like characters
		/// could not be swallowed.
		/// </remarks>
		/// <param name="message">The raw message from the native plugin.</param>
		public static GoogleSignInResult Parse(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				// Guarding this is not theoretical: the previous HandleResponse called
				// message.StartsWith on it and threw a NullReferenceException.
				return Error("The Google Sign-In plugin returned an empty response.");
			}

			string detail;

			if (TryMatchSentinel(message, SENTINEL_CANCELED, out detail))
			{
				return Cancelled(detail);
			}

			if (TryMatchSentinel(message, SENTINEL_NO_CREDENTIAL, out detail) ||
				TryMatchSentinel(message, SENTINEL_SIGN_IN_REQUIRED, out detail))
			{
				return NoCredential(detail ?? "No Google credential is available on this device.");
			}

			if (TryMatchSentinel(message, SENTINEL_UNAVAILABLE, out detail))
			{
				return Unavailable(detail ?? "Google Sign-In is unavailable.");
			}

			// signOut/revokeAccess acknowledgements. They arrive on their own callback method, so a
			// login request can never see them.
			if (TryMatchSentinel(message, SENTINEL_SIGNED_OUT, out detail) ||
				TryMatchSentinel(message, SENTINEL_REVOKED, out detail))
			{
				return new GoogleSignInResult { Status = GoogleSignInStatus.Success, Detail = message };
			}

			// UNKNOWN means the plugin got neither a token nor an exception. In practice it is almost
			// always a client ID that is not a *web* OAuth client, so no ID token was granted.
			if (TryMatchSentinel(message, SENTINEL_UNKNOWN, out detail))
			{
				return Error(detail ?? "Google Sign-In returned no ID token. Check that the configured " +
									   "client ID is a Web application OAuth client.");
			}

			// Android emits "EXCEPTION - msg", iOS emits "EXCEPTION msg". TryMatchSentinel strips
			// both shapes, so Detail is the same either way.
			if (TryMatchSentinel(message, SENTINEL_EXCEPTION, out detail))
			{
				return Error(detail ?? "Google Sign-In failed for an unspecified reason.");
			}

			return Success(message);
		}

		/// <summary>
		/// Match a sentinel at the start of a message and strip any trailing detail.
		/// </summary>
		/// <remarks>
		/// The sentinel must be followed by end of string, a space, or a dash. Without that check a
		/// prefix match alone would classify a hypothetical token beginning with "UNKNOWN..." as an
		/// error.
		/// </remarks>
		private static bool TryMatchSentinel(string message, string sentinel, out string detail)
		{
			detail = null;

			if (!message.StartsWith(sentinel, StringComparison.Ordinal))
			{
				return false;
			}

			if (message.Length == sentinel.Length)
			{
				return true;
			}

			var separator = message[sentinel.Length];
			if (separator != ' ' && separator != '-')
			{
				return false;
			}

			detail = message.Substring(sentinel.Length).Trim(' ', '-');
			if (detail.Length == 0)
			{
				detail = null;
			}

			return true;
		}
	}
}
