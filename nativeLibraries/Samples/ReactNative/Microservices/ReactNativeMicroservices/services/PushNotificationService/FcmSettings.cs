using System;
using System.Text.Json;

namespace Beamable.PushNotificationService
{
	/// <summary>
	/// Firebase Cloud Messaging (FCM) credentials, read from Realm Config — the
	/// Android counterpart to <see cref="ApnsSettings"/>.
	///
	/// Set this in Portal → your Realm → Config, under the namespace
	/// "<c>fcm_push</c>":
	///   • <c>service_account_json</c> — the <b>full JSON</b> of a Firebase service
	///     account key (Firebase Console → Project Settings → Service Accounts →
	///     "Generate new private key"). It contains <c>project_id</c>,
	///     <c>client_email</c>, <c>private_key</c> and <c>token_uri</c>.
	///
	/// Storing the whole JSON in one key mirrors how APNs stores the full <c>.p8</c>
	/// PEM in <c>apns_push.auth_key</c>: the secret lives only in Realm Config, never
	/// in the repo or the client, and differs per realm. Unlike APNs there is no
	/// sandbox/production split — FCM has a single endpoint.
	/// </summary>
	public class FcmSettings
	{
		public string ProjectId;
		public string ClientEmail;
		public string PrivateKeyPem;
		public string TokenUri;

		public const string Namespace = "fcm_push";

		/// <summary>OAuth2 scope required to send messages through the FCM HTTP v1 API.</summary>
		public const string MessagingScope = "https://www.googleapis.com/auth/firebase.messaging";

		private const string DefaultTokenUri = "https://oauth2.googleapis.com/token";

		/// <summary>The FCM HTTP v1 "send" endpoint for this project.</summary>
		public string SendUrl => $"https://fcm.googleapis.com/v1/projects/{ProjectId}/messages:send";

		/// <summary>
		/// A secret-free, log-safe one-line summary of what parsed out of the pasted JSON —
		/// confirms project/email/key shape without ever revealing the private key.
		/// </summary>
		public string DescribeSafely()
		{
			var key = PrivateKeyPem ?? "";
			var looksLikePem = key.Contains("BEGIN") && key.Contains("PRIVATE KEY");
			return $"project_id='{ProjectId}', client_email='{ClientEmail}', token_uri='{TokenUri}', " +
				   $"private_key=[{(looksLikePem ? "PEM" : "NOT-PEM")}, {key.Length} chars]";
		}

		/// <summary>
		/// Builds and validates the credentials from a setting getter (typically the
		/// realm-config namespace's <c>GetSetting</c>). Reads the <c>service_account_json</c>
		/// blob, parses it, and throws a clear error if the blob is missing, unparseable,
		/// or missing a required field — so the caller can surface a "not configured"
		/// message instead of failing mid-send.
		/// </summary>
		public static FcmSettings FromGetter(Func<string, string> getSetting)
		{
			var json = getSetting("service_account_json");
			if (string.IsNullOrWhiteSpace(json))
				throw new Exception($"missing Realm Config '{Namespace}.service_account_json'");

			JsonElement root;
			try
			{
				using var doc = JsonDocument.Parse(json);
				root = doc.RootElement.Clone();
			}
			catch (Exception ex)
			{
				throw new Exception($"'{Namespace}.service_account_json' is not valid JSON: {ex.Message}");
			}

			var settings = new FcmSettings
			{
				ProjectId = ReadString(root, "project_id"),
				ClientEmail = ReadString(root, "client_email"),
				PrivateKeyPem = NormalizePem(ReadString(root, "private_key")),
				TokenUri = ReadString(root, "token_uri"),
			};

			if (string.IsNullOrWhiteSpace(settings.TokenUri))
				settings.TokenUri = DefaultTokenUri;

			Require(settings.ProjectId, "project_id");
			Require(settings.ClientEmail, "client_email");
			Require(settings.PrivateKeyPem, "private_key");

			return settings;
		}

		private static string ReadString(JsonElement root, string property) =>
			root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
				? value.GetString()
				: null;

		/// <summary>
		/// The service-account <c>private_key</c> embeds newlines as the literal
		/// characters "\n" inside the JSON string; turn them into real newlines so
		/// <see cref="System.Security.Cryptography.RSA.ImportFromPem"/> accepts the PEM.
		/// (Same normalization trick as <c>ApnsClient.LoadKey</c>.)
		/// </summary>
		private static string NormalizePem(string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return key;
			key = key.Trim();
			return key.Contains("\\n") ? key.Replace("\\n", "\n") : key;
		}

		private static void Require(string value, string key)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new Exception($"'{Namespace}.service_account_json' is missing '{key}'");
		}
	}
}
