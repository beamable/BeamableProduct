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
	/// A single offer carried by a campaign push (§3.3 <c>offers[]</c>). <see cref="customData"/>
	/// is free-form (typed <c>T</c> at the SDK layer) and travels as opaque JSON across the
	/// native bridge, so it is modeled here as a JSON object string.
	/// </summary>
	[Serializable]
	public class PushOffer
	{
		public string itemId;
		public string value;        // "string|number" in the schema — carried as a string on the wire
		public string customData;   // free-form JSON object, as a string (e.g. {"k":"v"})
	}

	/// <summary>
	/// The notification content to deliver, plus the optional §3.3 Notification Intent Data
	/// (campaign context). All campaign fields are additive/optional — a plain
	/// title/body/deepLink message (the original shape) keeps working unchanged.
	///
	/// On the wire the schema is embedded as a FLAT string→string map (Decision Q3 "stringify"):
	/// scalars as plain strings, <see cref="offers"/> and <see cref="campaignData"/> as
	/// JSON-encoded strings — into FCM <c>data</c> and APNs <c>userInfo</c> identically.
	/// </summary>
	public class PushMessage
	{
		public string title;
		public string body;
		public string deepLink;     // canonical key: "deeplink"

		// --- §3.3 Notification Intent Data (all optional) ---
		public string campaignId;
		public string nodeId;
		public string gamerTag;     // Beamable dbid (the target player)
		public string accountId;
		public string cidPid;       // "<cid>.<pid>" realm scope
		public List<PushOffer> offers;          // optional array
		public string campaignData; // free-form JSON object, as a string

		/// <summary>
		/// Builds the §3.3 Notification Intent Data as a flat string→string map (Decision Q3):
		/// scalar fields as plain strings; <see cref="offers"/> and <see cref="campaignData"/> as
		/// JSON-encoded strings. The canonical deeplink key is <c>deeplink</c>. Empty/null fields
		/// are omitted so an un-tagged message produces an empty map (and a plain message stays
		/// byte-identical to the old payload aside from the existing deeplink key).
		/// Used for both FCM <c>data</c> and APNs <c>userInfo</c> so engine code is identical.
		/// </summary>
		public void WriteIntentData(IDictionary<string, object> map)
		{
			if (!string.IsNullOrWhiteSpace(campaignId)) map["campaignId"] = campaignId;
			if (!string.IsNullOrWhiteSpace(nodeId)) map["nodeId"] = nodeId;
			if (!string.IsNullOrWhiteSpace(gamerTag)) map["gamerTag"] = gamerTag;
			if (!string.IsNullOrWhiteSpace(accountId)) map["accountId"] = accountId;
			if (!string.IsNullOrWhiteSpace(cidPid)) map["cidPid"] = cidPid;
			if (!string.IsNullOrWhiteSpace(deepLink)) map["deeplink"] = deepLink;

			if (offers != null && offers.Count > 0)
				map["offers"] = JsonSerializer.Serialize(offers);

			// campaignData is already a JSON object string — passed through verbatim.
			if (!string.IsNullOrWhiteSpace(campaignData)) map["campaignData"] = campaignData;
		}
	}

	/// <summary>Outcome of a single device delivery (shared by every push provider).</summary>
	public class PushSendOutcome
	{
		public bool ok;
		public string reason;
		public bool tokenIsInvalid; // the provider says this token is dead → prune it
	}

	/// <summary>
	/// Sends notifications to Apple's APNs over HTTP/2 using token-based
	/// authentication (a JWT signed with your .p8 key — no per-push certificate).
	///
	/// The <see cref="HttpClient"/> and the signed provider JWT are cached in
	/// <c>static</c> fields: Apple throttles ("TooManyProviderTokenUpdates") if you
	/// mint a new JWT too often, and the token is valid for up to an hour, so we
	/// reuse one for ~50 minutes across every request and instance.
	/// </summary>
	public class ApnsClient
	{
		// One shared HTTP/2 client for the whole process.
		private static readonly HttpClient Http = new(new SocketsHttpHandler
		{
			// APNs supports many requests on one HTTP/2 connection; keep it warm.
			PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
			EnableMultipleHttp2Connections = true,
		})
		{
			DefaultRequestVersion = HttpVersion.Version20,
			DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
		};

		// Cached provider JWT, guarded by _jwtLock.
		private static readonly object JwtLock = new();
		private static string _cachedJwt;
		private static string _cachedJwtFingerprint;
		private static DateTimeOffset _jwtIssuedAt;
		private static readonly TimeSpan JwtLifetime = TimeSpan.FromMinutes(50);

		/// <summary>Sends one push to one device.</summary>
		public async Task<PushSendOutcome> Send(ApnsSettings settings, DeviceInfo device, PushMessage message)
		{
			string jwt;
			try
			{
				jwt = GetProviderToken(settings);
			}
			catch (Exception ex)
			{
				return new PushSendOutcome { ok = false, reason = $"failed to sign provider token: {ex.Message}" };
			}

			var environment = settings.ResolveEnvironment(device.environment);
			var url = $"https://{ApnsEnvironment.HostFor(environment)}/3/device/{device.token}";
			var payload = BuildPayload(message);

			using var request = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Version = HttpVersion.Version20,
				VersionPolicy = HttpVersionPolicy.RequestVersionExact,
				Content = new StringContent(payload, Encoding.UTF8, "application/json"),
			};
			request.Headers.TryAddWithoutValidation("authorization", $"bearer {jwt}");
			request.Headers.TryAddWithoutValidation("apns-topic", settings.BundleId);
			request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
			request.Headers.TryAddWithoutValidation("apns-priority", "10");

			try
			{
				using var response = await Http.SendAsync(request);
				if (response.StatusCode == HttpStatusCode.OK)
					return new PushSendOutcome { ok = true };

				var bodyText = await response.Content.ReadAsStringAsync();
				var reason = ExtractReason(bodyText) ?? response.StatusCode.ToString();
				var dead = response.StatusCode == HttpStatusCode.Gone // 410 Unregistered
					|| reason is "BadDeviceToken" or "Unregistered" or "DeviceTokenNotForTopic";

				BeamableLogger.LogWarning("APNs rejected push ({status}): {reason}", (int)response.StatusCode, reason);
				return new PushSendOutcome { ok = false, reason = reason, tokenIsInvalid = dead };
			}
			catch (Exception ex)
			{
				return new PushSendOutcome { ok = false, reason = $"transport error: {ex.Message}" };
			}
		}

		/// <summary>Builds the APNs JSON payload. Omits empty fields; carries an optional deep link.</summary>
		private static string BuildPayload(PushMessage message)
		{
			var alert = new Dictionary<string, object>();
			if (!string.IsNullOrWhiteSpace(message.title)) alert["title"] = message.title;
			if (!string.IsNullOrWhiteSpace(message.body)) alert["body"] = message.body;

			var aps = new Dictionary<string, object>
			{
				["alert"] = alert,
				["sound"] = "default",
				// Wake the Notification Service Extension on delivery — even when the app is
				// backgrounded/killed — so it can run closed-app analytics (the webhook POST).
				// Without "mutable-content": 1 the NSE never runs and the closed-app POST won't
				// fire (the alert still displays). Requires apns-push-type: alert (set above).
				["mutable-content"] = 1,
			};

			var root = new Dictionary<string, object> { ["aps"] = aps };

			// §3.3 Notification Intent Data goes in userInfo (the top-level keys alongside "aps"),
			// stringified to match Android's FCM data map exactly (Decision Q3). This writes the
			// canonical "deeplink" key (the app already reads it tolerantly: deepLink/deeplink/deep_link).
			message.WriteIntentData(root);

			// Back-compat: keep the legacy "deepLink" key the original payload emitted.
			if (!string.IsNullOrWhiteSpace(message.deepLink))
				root["deepLink"] = message.deepLink; // the app reads this on tap to route

			return JsonSerializer.Serialize(root);
		}

		// --- Provider JWT ---------------------------------------------------

		private static string GetProviderToken(ApnsSettings settings)
		{
			var fingerprint = $"{settings.TeamId}:{settings.KeyId}";
			lock (JwtLock)
			{
				var fresh = _cachedJwt != null
					&& _cachedJwtFingerprint == fingerprint
					&& DateTimeOffset.UtcNow - _jwtIssuedAt < JwtLifetime;
				if (fresh) return _cachedJwt;

				_cachedJwt = SignProviderToken(settings);
				_cachedJwtFingerprint = fingerprint;
				_jwtIssuedAt = DateTimeOffset.UtcNow;
				return _cachedJwt;
			}
		}

		private static string SignProviderToken(ApnsSettings settings)
		{
			var header = $"{{\"alg\":\"ES256\",\"kid\":\"{settings.KeyId}\",\"typ\":\"JWT\"}}";
			var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var claims = $"{{\"iss\":\"{settings.TeamId}\",\"iat\":{iat}}}";

			var signingInput = $"{Base64Url(Encoding.UTF8.GetBytes(header))}.{Base64Url(Encoding.UTF8.GetBytes(claims))}";

			using var ecdsa = LoadKey(settings.AuthKeyP8);
			var signature = ecdsa.SignData(
				Encoding.UTF8.GetBytes(signingInput),
				HashAlgorithmName.SHA256,
				DSASignatureFormat.IeeeP1363FixedFieldConcatenation); // JWS needs raw r||s, not DER

			return $"{signingInput}.{Base64Url(signature)}";
		}

		/// <summary>Imports the .p8 (PKCS#8) key whether it's full PEM, escaped, or bare base64.</summary>
		private static ECDsa LoadKey(string authKey)
		{
			var key = authKey.Trim();
			// Realm Config UIs often store newlines as the literal characters "\n".
			if (key.Contains("\\n")) key = key.Replace("\\n", "\n");

			var ecdsa = ECDsa.Create();
			if (key.Contains("BEGIN"))
			{
				ecdsa.ImportFromPem(key);
			}
			else
			{
				// Bare base64 of the PKCS#8 DER (header/footer stripped).
				var der = Convert.FromBase64String(key.Replace("\n", "").Replace("\r", "").Trim());
				ecdsa.ImportPkcs8PrivateKey(der, out _);
			}
			return ecdsa;
		}

		private static string Base64Url(byte[] bytes) =>
			Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

		private static string ExtractReason(string apnsErrorBody)
		{
			if (string.IsNullOrWhiteSpace(apnsErrorBody)) return null;
			try
			{
				using var doc = JsonDocument.Parse(apnsErrorBody);
				return doc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
			}
			catch
			{
				return null;
			}
		}
	}
}
