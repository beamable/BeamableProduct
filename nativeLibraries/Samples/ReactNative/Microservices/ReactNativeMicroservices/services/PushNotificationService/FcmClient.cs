using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;

namespace Beamable.PushNotificationService
{
	/// <summary>
	/// Sends notifications to Android (and any FCM client) through Firebase Cloud
	/// Messaging's HTTP v1 API — the Android counterpart to <see cref="ApnsClient"/>.
	///
	/// FCM has no per-push token like APNs. Instead we authenticate with a short-lived
	/// OAuth2 access token, obtained by signing a JWT (RS256) with the service account's
	/// private key and exchanging it at Google's token endpoint. Like the APNs provider
	/// JWT, the access token is valid for ~1 hour, so the <see cref="HttpClient"/> and the
	/// access token are cached in <c>static</c> fields and reused for ~50 minutes across
	/// every request and instance.
	/// </summary>
	public class FcmClient
	{
		// One shared client for the whole process. FCM is plain HTTPS (HTTP/1.1 is fine).
		private static readonly HttpClient Http = new(new SocketsHttpHandler
		{
			PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
		});

		// Cached OAuth access token, guarded by _tokenLock.
		private static readonly object TokenLock = new();
		private static string _cachedToken;
		private static string _cachedTokenFingerprint;
		private static DateTimeOffset _tokenIssuedAt;
		private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(50);

		/// <summary>Sends one push to one device.</summary>
		public async Task<PushSendOutcome> Send(FcmSettings settings, DeviceInfo device, PushMessage message)
		{
			string accessToken;
			try
			{
				accessToken = await GetAccessToken(settings);
			}
			catch (Exception ex)
			{
				return new PushSendOutcome { ok = false, reason = $"failed to get FCM access token: {ex.Message}" };
			}

			var payload = BuildPayload(device.token, message);

			using var request = new HttpRequestMessage(HttpMethod.Post, settings.SendUrl)
			{
				Content = new StringContent(payload, Encoding.UTF8, "application/json"),
			};
			request.Headers.TryAddWithoutValidation("authorization", $"Bearer {accessToken}");

			try
			{
				using var response = await Http.SendAsync(request);
				if (response.StatusCode == HttpStatusCode.OK)
					return new PushSendOutcome { ok = true };

				var bodyText = await response.Content.ReadAsStringAsync();
				var reason = ExtractError(bodyText) ?? response.StatusCode.ToString();
				// FCM marks a token dead with these codes (404 UNREGISTERED, 400 INVALID_ARGUMENT).
				var dead = reason is "UNREGISTERED" or "INVALID_ARGUMENT"
					|| response.StatusCode == HttpStatusCode.NotFound;

				BeamableLogger.LogWarning("FCM rejected push ({status}): {reason}", (int)response.StatusCode, reason);
				return new PushSendOutcome { ok = false, reason = reason, tokenIsInvalid = dead };
			}
			catch (Exception ex)
			{
				return new PushSendOutcome { ok = false, reason = $"transport error: {ex.Message}" };
			}
		}

		/// <summary>
		/// Builds the FCM HTTP v1 message. Note that <c>data</c> values must be strings,
		/// so the optional deep link is carried as a string just like in the APNs payload.
		/// </summary>
		private static string BuildPayload(string deviceToken, PushMessage message)
		{
			var notification = new Dictionary<string, object>();
			if (!string.IsNullOrWhiteSpace(message.title)) notification["title"] = message.title;
			if (!string.IsNullOrWhiteSpace(message.body)) notification["body"] = message.body;

			var msg = new Dictionary<string, object>
			{
				["token"] = deviceToken,
				["notification"] = notification,
				["android"] = new Dictionary<string, object> { ["priority"] = "high" },
			};

			if (!string.IsNullOrWhiteSpace(message.deepLink))
				msg["data"] = new Dictionary<string, object> { ["deepLink"] = message.deepLink };

			var root = new Dictionary<string, object> { ["message"] = msg };
			return JsonSerializer.Serialize(root);
		}

		// --- OAuth2 access token --------------------------------------------

		private static async Task<string> GetAccessToken(FcmSettings settings)
		{
			lock (TokenLock)
			{
				var fresh = _cachedToken != null
					&& _cachedTokenFingerprint == settings.ClientEmail
					&& DateTimeOffset.UtcNow - _tokenIssuedAt < TokenLifetime;
				if (fresh) return _cachedToken;
			}

			var token = await RequestAccessToken(settings);

			lock (TokenLock)
			{
				_cachedToken = token;
				_cachedTokenFingerprint = settings.ClientEmail;
				_tokenIssuedAt = DateTimeOffset.UtcNow;
				return _cachedToken;
			}
		}

		/// <summary>Mints a JWT assertion and exchanges it for an OAuth2 access token.</summary>
		private static async Task<string> RequestAccessToken(FcmSettings settings)
		{
			var assertion = SignAssertion(settings);

			using var request = new HttpRequestMessage(HttpMethod.Post, settings.TokenUri)
			{
				Content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
					["assertion"] = assertion,
				}),
			};

			using var response = await Http.SendAsync(request);
			var body = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
				throw new Exception($"token endpoint returned {(int)response.StatusCode}: {body}");

			using var doc = JsonDocument.Parse(body);
			if (doc.RootElement.TryGetProperty("access_token", out var token) && token.ValueKind == JsonValueKind.String)
				return token.GetString();

			throw new Exception($"token endpoint response had no access_token: {body}");
		}

		/// <summary>
		/// Imports the service-account RSA key to confirm it's well-formed. The #1 risk when
		/// pasting the service-account JSON into Realm Config is the <c>private_key</c> losing
		/// its newlines — this throws a clear message in that case. Used by the admin
		/// <c>CheckFcmConfig</c> diagnostic; does no network I/O.
		/// </summary>
		public static void EnsureKeyLoads(FcmSettings settings)
		{
			try
			{
				using var rsa = RSA.Create();
				rsa.ImportFromPem(settings.PrivateKeyPem);
			}
			catch (Exception ex)
			{
				throw new Exception(
					$"private_key could not be parsed as a PEM RSA key — check that the pasted JSON kept its newlines: {ex.Message}");
			}
		}

		/// <summary>Builds and RS256-signs the JWT bearer assertion for the token exchange.</summary>
		private static string SignAssertion(FcmSettings settings)
		{
			var header = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
			var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var exp = iat + 3600;
			var claims =
				$"{{\"iss\":\"{settings.ClientEmail}\",\"scope\":\"{FcmSettings.MessagingScope}\"," +
				$"\"aud\":\"{settings.TokenUri}\",\"iat\":{iat},\"exp\":{exp}}}";

			var signingInput = $"{Base64Url(Encoding.UTF8.GetBytes(header))}.{Base64Url(Encoding.UTF8.GetBytes(claims))}";

			using var rsa = RSA.Create();
			rsa.ImportFromPem(settings.PrivateKeyPem);
			var signature = rsa.SignData(
				Encoding.UTF8.GetBytes(signingInput),
				HashAlgorithmName.SHA256,
				RSASignaturePadding.Pkcs1);

			return $"{signingInput}.{Base64Url(signature)}";
		}

		private static string Base64Url(byte[] bytes) =>
			Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

		/// <summary>
		/// Pulls the FCM error code out of an HTTP v1 error body. Prefers the
		/// FCM-specific <c>error.details[].errorCode</c>, falling back to <c>error.status</c>.
		/// </summary>
		private static string ExtractError(string errorBody)
		{
			if (string.IsNullOrWhiteSpace(errorBody)) return null;
			try
			{
				using var doc = JsonDocument.Parse(errorBody);
				if (!doc.RootElement.TryGetProperty("error", out var error)) return null;

				if (error.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
				{
					foreach (var detail in details.EnumerateArray())
					{
						if (detail.TryGetProperty("errorCode", out var code) && code.ValueKind == JsonValueKind.String)
							return code.GetString();
					}
				}

				return error.TryGetProperty("status", out var status) ? status.GetString() : null;
			}
			catch
			{
				return null;
			}
		}
	}
}
