using System;

namespace Beamable.PushNotificationService
{
	/// <summary>Which APNs environment a device's token belongs to.</summary>
	public static class ApnsEnvironment
	{
		public const string Sandbox = "sandbox";
		public const string Production = "production";

		/// <summary>The APNs host for an environment.</summary>
		public static string HostFor(string environment) =>
			environment == Sandbox ? "api.sandbox.push.apple.com" : "api.push.apple.com";
	}

	/// <summary>
	/// APNs token-based-auth credentials, read from Realm Config.
	///
	/// Set these in Portal → your Realm → Config, under the namespace
	/// "<c>apns_push</c>":
	///   • <c>auth_key</c>      — contents of your AuthKey_XXXX.p8 file (the full PEM,
	///                            "-----BEGIN PRIVATE KEY-----" … included).
	///   • <c>key_id</c>        — the 10-char Key ID of that .p8 key.
	///   • <c>team_id</c>       — your 10-char Apple Developer Team ID.
	///   • <c>bundle_id</c>     — the app's bundle id (used as the APNs topic),
	///                            e.g. "com.beamable.rnsample".
	///   • <c>default_environment</c> — optional: "sandbox" (default) or "production".
	///
	/// Keeping these in Realm Config (not in source) means the .p8 secret never
	/// ships in the repo or the client, and differs per realm.
	/// </summary>
	public class ApnsSettings
	{
		public string AuthKeyP8;
		public string KeyId;
		public string TeamId;
		public string BundleId;
		public string DefaultEnvironment;

		public const string Namespace = "apns_push";

		/// <summary>
		/// Resolves the APNs environment for a device — its own stored value, or the
		/// realm default when the device didn't specify one.
		/// </summary>
		public string ResolveEnvironment(string deviceEnvironment) =>
			!string.IsNullOrWhiteSpace(deviceEnvironment) ? deviceEnvironment : DefaultEnvironment;

		/// <summary>
		/// Builds and validates the credentials from a setting getter (typically the
		/// realm-config namespace's <c>GetSetting</c>). Throws if a required value is
		/// missing so the caller can surface a clear "not configured" error.
		/// </summary>
		public static ApnsSettings FromGetter(Func<string, string> getSetting)
		{
			var settings = new ApnsSettings
			{
				AuthKeyP8 = getSetting("auth_key"),
				KeyId = getSetting("key_id"),
				TeamId = getSetting("team_id"),
				BundleId = getSetting("bundle_id"),
				DefaultEnvironment = NormalizeEnv(getSetting("default_environment")),
			};

			Require(settings.AuthKeyP8, "auth_key");
			Require(settings.KeyId, "key_id");
			Require(settings.TeamId, "team_id");
			Require(settings.BundleId, "bundle_id");

			return settings;
		}

		private static string NormalizeEnv(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw)) return ApnsEnvironment.Sandbox;
			var e = raw.Trim().ToLowerInvariant();
			return e is "production" or "prod" ? ApnsEnvironment.Production : ApnsEnvironment.Sandbox;
		}

		private static void Require(string value, string key)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new Exception($"missing Realm Config '{Namespace}.{key}'");
		}
	}
}
