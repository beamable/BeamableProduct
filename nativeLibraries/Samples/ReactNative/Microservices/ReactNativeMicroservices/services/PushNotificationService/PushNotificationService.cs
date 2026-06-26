using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Beamable.Api.Analytics;
using Beamable.Common;
using Beamable.Common.Api.Stats;
using Beamable.Common.Content;
using Beamable.Server;

namespace Beamable.PushNotificationService
{
	/// <summary>
	/// A self-contained microservice that demonstrates remote push notifications
	/// through both Apple's APNs (iOS) and Firebase Cloud Messaging (Android), end to end:
	///
	///   1. <see cref="RegisterDeviceToken"/>      — a player registers the device token their
	///      app received from the OS, tagged with the platform (one token per device).
	///   2. <see cref="SendCampaignPushToSelf"/>   — a player sends a real remote push to
	///      their own registered device(s) — the simplest thing to demo from the app.
	///   3. <see cref="SendCampaignPushToPlayer"/> — an admin/back-office tool sends a push
	///      to any player by id.
	///
	/// Device tokens are stored as a <b>private</b> per-player stat (see
	/// <see cref="DeviceTokenStore"/>) — no MongoDB needed for this key/value data.
	/// Each stored device carries a <see cref="DeviceInfo.platform"/> ("apns" or "fcm");
	/// <see cref="DeliverToPlayer"/> routes each one to the right client:
	/// <see cref="ApnsClient"/> (Apple, HTTP/2 + .p8 JWT) or <see cref="FcmClient"/>
	/// (Firebase HTTP v1 + service-account OAuth). The per-provider credentials live in
	/// Realm Config (Portal → Realm → Config) under the "<c>apns_push</c>" and
	/// "<c>fcm_push</c>" namespaces — see <see cref="ApnsSettings"/> and <see cref="FcmSettings"/>.
	/// </summary>
	public partial class PushNotificationService : Microservice
	{
		// --- Registration ---------------------------------------------------

		/// <summary>
		/// Registers (or refreshes) the calling player's device token. The OS hands the
		/// app a token (iOS via the native SDK's <c>tokenReceived</c> event, Android via
		/// FCM); the app forwards it here. Safe to call repeatedly — the same token is
		/// de-duplicated and its timestamp refreshed.
		/// </summary>
		/// <param name="token">The device token from the OS (APNs hex token, or FCM registration token).</param>
		/// <param name="environment">APNs only: "sandbox" (dev/TestFlight) or "production" (App Store). Defaults to the realm's configured default. Ignored for FCM.</param>
		/// <param name="platform">"apns" (default, iOS) or "fcm" (Android). Empty → "apns".</param>
		[ClientCallable]
		public async Task<RegisterResult> RegisterDeviceToken(string token, string environment, string platform)
		{
			if (string.IsNullOrWhiteSpace(token))
				return new RegisterResult { success = false, message = "A non-empty device token is required.", deviceCount = 0 };

			var plat = NormalizePlatform(platform);
			var env = NormalizeEnvironment(environment);
			var devices = await LoadDevices(Context.UserId);

			var existing = devices.FirstOrDefault(d => d.token == token);
			if (existing != null)
			{
				existing.platform = plat;
				existing.environment = env;
				existing.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			}
			else
			{
				devices.Add(new DeviceInfo
				{
					token = token,
					platform = plat,
					environment = env,
					updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				});
			}

			await SaveDevices(Context.UserId, devices);
			BeamableLogger.Log("Registered APNs device for player {player} ({count} total)", Context.UserId, devices.Count);
			return new RegisterResult { success = true, deviceCount = devices.Count, message = "Device registered." };
		}

		/// <summary>Removes one of the calling player's device tokens (e.g. on logout).</summary>
		[ClientCallable]
		public async Task<UnregisterResult> UnregisterDeviceToken(string token)
		{
			var devices = await LoadDevices(Context.UserId);
			var removed = devices.RemoveAll(d => d.token == token);
			await SaveDevices(Context.UserId, devices);
			return new UnregisterResult
			{
				success = removed > 0,
				deviceCount = devices.Count,
				message = removed > 0 ? "Device removed." : "Token was not registered.",
			};
		}

		/// <summary>Lists the calling player's registered devices (tokens are masked in the response).</summary>
		[ClientCallable]
		public async Task<DeviceList> ListMyDevices()
		{
			var devices = await LoadDevices(Context.UserId);
			return new DeviceList
			{
				devices = devices.Select(d => new DeviceInfo
				{
					token = Mask(d.token),
					platform = NormalizePlatform(d.platform),
					environment = d.environment,
					updatedAt = d.updatedAt,
				}).ToList(),
			};
		}

		// --- Analytics funnel webhook ---------------------------------------

		private const string SlackWebhookUrl =
			"https://hooks.slack.com/triggers/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";

		// Reused across invocations (SocketsHttpHandler mirrors ApnsClient/FcmClient).
		private static readonly HttpClient WebhookHttp = new(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(5),
		});

		// Funnel payloads nest stringified JSON (offerData/campaignData are JSON-object strings).
		// The relaxed encoder renders their inner quotes as \" rather than System.Text.Json's default
		// " escaping, so the webhook/analytics payload stays human-readable.
		private static readonly JsonSerializerOptions FunnelJson = new()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};

		/// <summary>
		/// Receives analytics funnel data as a single stringified-JSON payload and forwards it to a
		/// Slack webhook as <c>{"message": &lt;payload&gt;}</c>. Exposed as <c>[Callable]</c> so it does
		/// NOT require admin auth — any caller (e.g. the app's analytics layer) can post funnel data.
		/// </summary>
		/// <param name="funnelData">The funnel payload — a stringified JSON object.</param>
		[Callable]
		public Task<WebhookResult> ForwardFunnelToSlack(string funnelData) => PostFunnelToSlack(funnelData);

		/// <summary>
		/// POSTs <paramref name="funnelData"/> to the Slack webhook as <c>{"message": &lt;payload&gt;}</c>.
		/// Best-effort: returns the outcome instead of throwing. Shared by the <see cref="ForwardFunnelToSlack"/>
		/// endpoint and the server-side "Sent" funnel emission (<see cref="EmitSentEvent"/>).
		/// </summary>
		private static async Task<WebhookResult> PostFunnelToSlack(string funnelData)
		{
			// Slack trigger expects a flat object whose values are strings — wrap the (already
			// stringified) funnel JSON as the "message" value rather than embedding a nested object.
			var body = JsonSerializer.Serialize(new { message = funnelData ?? string.Empty }, FunnelJson);
			using var request = new HttpRequestMessage(HttpMethod.Post, SlackWebhookUrl)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			};
			try
			{
				using var response = await WebhookHttp.SendAsync(request);
				var ok = response.IsSuccessStatusCode;
				if (!ok)
					BeamableLogger.LogWarning("Slack funnel webhook returned HTTP {code}", (int)response.StatusCode);
				return new WebhookResult { success = ok, statusCode = (int)response.StatusCode };
			}
			catch (Exception ex)
			{
				// Best-effort — never throw back to the caller for a webhook failure.
				BeamableLogger.LogWarning("Slack funnel webhook POST failed: {msg}", ex.Message);
				return new WebhookResult { success = false, statusCode = 0, message = ex.Message };
			}
		}

		// --- Sending --------------------------------------------------------

		/// <summary>
		/// Sends a remote push to every device the calling player has registered, carrying the
		/// §3.3 Notification Intent Data (campaign/node/offers/campaignData). The easiest
		/// end-to-end demo: register on a device, then call this from the same device. Requires a
		/// physical iOS device (APNs does not deliver to the Simulator) and valid APNs credentials
		/// in Realm Config. All campaign fields are optional — an empty request reduces to a plain
		/// title/body/deepLink push. When <c>campaignId</c> and <c>nodeId</c> are both present the
		/// microservice also emits a funnel "Sent" analytics event (§4.4).
		/// </summary>
		[ClientCallable]
		public Task<SendResult> SendCampaignPushToSelf(PushCampaignRequest request)
		{
			request ??= new PushCampaignRequest();
			return DeliverToPlayer(Context.UserId, request.title, request.body, request.deepLink, request.ToContext());
		}

		/// <summary>
		/// Back-office endpoint: send a remote push to a specific player by id, carrying the §3.3
		/// Notification Intent Data. Exposed as <c>[ServerCallable]</c> so the Portal extension can
		/// call it — that still requires the "<c>*</c>" (admin) scope, but unlike
		/// <c>[AdminOnlyCallable]</c> it does not require a logged-in player, which a Portal
		/// extension's session does not carry. The target player id is supplied separately; the
		/// rest of the campaign context rides in <paramref name="request"/>. All campaign fields
		/// are optional; when <c>campaignId</c> + <c>nodeId</c> are present a funnel "Sent" event
		/// is emitted (§4.4).
		/// </summary>
		[ServerCallable]
		public async Task<AdminSendResult> SendCampaignPushToPlayer(long playerId, PushCampaignRequest request)
		{
			request ??= new PushCampaignRequest();
			var r = await DeliverToPlayer(playerId, request.title, request.body, request.deepLink, request.ToContext());
			return new AdminSendResult
			{
				success = r.success,
				attempted = r.attempted,
				succeeded = r.succeeded,
				failed = r.failed,
				messages = r.messages,
			};
		}

		/// <summary>
		/// Admin/back-office endpoint: lists every player who has at least one registered
		/// device, with a small summary (device count, platforms, last-updated). Used by the
		/// Portal extension to pick a recipient for <see cref="SendCampaignPushToPlayer"/>.
		///
		/// Private per-player stats aren't enumerable, so we find the roster by searching the
		/// public marker stat (<c>push_devices != 0</c>) that <see cref="SaveDevices"/> keeps
		/// in sync, then load each player's private device list for the summary. Tokens are
		/// never returned.
		/// </summary>
		[ServerCallable]
		public async Task<RegisteredPlayerList> ListRegisteredPlayers()
		{
			// SearchStats lives on the concrete AbsStatsApi (admin-only), not the IStatsApi
			// interface that Services.Stats is typed as — so cast to reach it.
			if (Services.Stats is not AbsStatsApi search)
				return new RegisteredPlayerList { message = "Stats search is unavailable in this runtime." };

			// Stat search compares values as STRINGS, so a numeric "gt 0" never matches the
			// string-stored count. Use a string "neq 0" to select every player whose marker
			// is a non-zero count (the "0" markers left by a full unregister are excluded).
			var response = await search.SearchStats(
				PushStatDomain, PushStatPublicAccess, PushStatPlayerType,
				new List<Criteria> { new Criteria(PublicMarkerStatKey, "neq", "0") });

			var ids = response?.ids ?? Array.Empty<long>();
			var result = new RegisteredPlayerList();

			foreach (var id in ids)
			{
				var devices = await LoadDevices(id);
				if (devices.Count == 0) continue; // marker lagged behind a prune — skip

				// The player's game platform/device come from their private profile stats
				// (set by the game), not from push registration. Missing → empty string.
				var profile = await Services.Stats.GetFilteredStats(
					StatsDomainType.Game, StatsAccessType.Private, id,
					new[] { GamePlatformStatKey, GameDeviceStatKey });

				result.players.Add(new RegisteredPlayer
				{
					playerId = id,
					deviceCount = devices.Count,
					platforms = devices.Select(d => NormalizePlatform(d.platform)).Distinct().ToList(),
					lastUpdated = devices.Max(d => d.updatedAt),
					gamePlatform = profile.GetValueOrDefault(GamePlatformStatKey, ""),
					gameDevice = profile.GetValueOrDefault(GameDeviceStatKey, ""),
				});
			}

			return result;
		}

		/// <summary>
		/// Admin/diagnostic endpoint: verifies that <c>fcm_push.service_account_json</c> in Realm
		/// Config parses and that the private key actually loads — handy right after pasting the
		/// JSON into the Portal, since a mangled <c>private_key</c> is the usual failure. Returns a
		/// secret-free summary and never echoes the key. Also logs the same summary server-side.
		/// </summary>
		[ServerCallable]
		public async Task<FcmConfigStatus> CheckFcmConfig()
		{
			try
			{
				var settings = await LoadFcmSettings(); // also logs the safe summary
				FcmClient.EnsureKeyLoads(settings);     // throws if the RSA key can't be parsed
				return new FcmConfigStatus
				{
					configured = true,
					privateKeyLoaded = true,
					projectId = settings.ProjectId,
					clientEmail = settings.ClientEmail,
					tokenUri = settings.TokenUri,
					message = "fcm_push.service_account_json parsed and the private key loaded successfully.",
				};
			}
			catch (Exception ex)
			{
				BeamableLogger.LogWarning("FCM config check failed: {msg}", ex.Message);
				return new FcmConfigStatus { configured = false, privateKeyLoaded = false, message = ex.Message };
			}
		}

		// "game"/"public"/"player" — the domain/access/objectType the public marker is stored under
		// (matches StatsDomainType.Game + StatsAccessType.Public + the "player" object type).
		private const string PushStatDomain = "game";
		private const string PushStatPublicAccess = "public";
		private const string PushStatPlayerType = "player";

		// Player profile stats (game.private), set by the game — surfaced in the admin roster.
		private const string GamePlatformStatKey = "THORIUM_GAME_PLATFORM";
		private const string GameDeviceStatKey = "THORIUM_GAME_DEVICE";

		// --- Internals ------------------------------------------------------

		private async Task<SendResult> DeliverToPlayer(long playerId, string title, string body, string deepLink, PushCampaignContext campaign = null)
		{
			if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
				return new SendResult { success = false, attempted = 0, succeeded = 0, failed = 0, messages = { "title or body is required." } };

			var devices = await LoadDevices(playerId);
			var result = new SendResult { attempted = devices.Count };

			if (devices.Count == 0)
			{
				result.success = false;
				result.messages.Add($"Player {playerId} has no registered devices.");
				return result;
			}

			var apns = new ApnsClient();
			var fcm = new FcmClient();

			// The §3.3 Notification Intent Data, embedded in the provider payload (FCM data / APNs
			// userInfo). gamerTag defaults to the target player id when the caller didn't set it,
			// and cidPid defaults to this microservice's own realm scope ("<cid>.<pid>") so the
			// funnel "Sent" event always carries the cidPid the device-side Received/Opened stages
			// join on (they require it). The caller may still override it.
			campaign ??= new PushCampaignContext();
			var message = new PushMessage
			{
				title = title,
				body = body,
				deepLink = deepLink,
				campaignId = campaign.campaignId,
				nodeId = campaign.nodeId,
				gamerTag = string.IsNullOrWhiteSpace(campaign.gamerTag) ? playerId.ToString() : campaign.gamerTag,
				accountId = campaign.accountId,
				cidPid = string.IsNullOrWhiteSpace(campaign.cidPid) ? $"{Context.Cid}.{Context.Pid}" : campaign.cidPid,
				offers = campaign.offers,
				campaignData = campaign.campaignData,
			};
			var stale = new List<string>();

			// Provider settings are resolved lazily and cached for this call, so a player with
			// only FCM devices never fails on a missing apns_push config (and vice versa). A
			// missing config for one provider surfaces as a per-device message, not a hard abort.
			var apnsLoaded = false; ApnsSettings apnsSettings = null; string apnsError = null;
			async Task<(ApnsSettings settings, string error)> GetApns()
			{
				if (!apnsLoaded)
				{
					apnsLoaded = true;
					try { apnsSettings = await LoadApnsSettings(); }
					catch (Exception ex) { apnsError = $"APNs not configured: {ex.Message}"; }
				}
				return (apnsSettings, apnsError);
			}

			var fcmLoaded = false; FcmSettings fcmSettings = null; string fcmError = null;
			async Task<(FcmSettings settings, string error)> GetFcm()
			{
				if (!fcmLoaded)
				{
					fcmLoaded = true;
					try { fcmSettings = await LoadFcmSettings(); }
					catch (Exception ex) { fcmError = $"FCM not configured: {ex.Message}"; }
				}
				return (fcmSettings, fcmError);
			}

			foreach (var device in devices)
			{
				PushSendOutcome outcome;
				if (NormalizePlatform(device.platform) == PushPlatform.Fcm)
				{
					var (settings, error) = await GetFcm();
					outcome = settings != null
						? await fcm.Send(settings, device, message)
						: new PushSendOutcome { ok = false, reason = error };
				}
				else
				{
					var (settings, error) = await GetApns();
					outcome = settings != null
						? await apns.Send(settings, device, message)
						: new PushSendOutcome { ok = false, reason = error };
				}

				if (outcome.ok)
				{
					result.succeeded++;
				}
				else
				{
					result.failed++;
					result.messages.Add($"{Mask(device.token)}: {outcome.reason}");
					if (outcome.tokenIsInvalid) stale.Add(device.token);
				}
			}

			// §4.4 — funnel "Sent" event, emitted ONCE per (player, send) once at least one device
			// send succeeded — not once per device. Only fires when the message carries both
			// campaignId and nodeId (§4.2 tracked-campaign rule); otherwise the push is untracked
			// and we skip silently.
			if (result.succeeded > 0)
				EmitSentEvent(playerId, message);

			// The provider told us these tokens are dead (APNs Unregistered/BadDeviceToken,
			// FCM UNREGISTERED/INVALID_ARGUMENT) — prune them so we stop delivering to them.
			if (stale.Count > 0)
			{
				devices.RemoveAll(d => stale.Contains(d.token));
				await SaveDevices(playerId, devices);
				result.messages.Add($"Pruned {stale.Count} invalid token(s).");
			}

			result.success = result.succeeded > 0;
			return result;
		}

		/// <summary>
		/// §4.4 / §4.6 — emits a funnel "Sent" <see cref="CoreEvent"/> via
		/// <c>Services.Analytics</c> (<see cref="Beamable.Server.Api.Analytics.IMicroserviceAnalyticsService"/>),
		/// once per logical send (once at least one of the player's devices accepted the push) —
		/// not once per device. Fires only when the message carries both <c>campaignId</c> and
		/// <c>nodeId</c> (§4.2); an untracked push is skipped silently.
		///
		/// The event is a <c>CoreEvent</c> (category "notification_funnel", eventName "Sent")
		/// whose params follow §4.6: campaignId, nodeId, gamerTag (the target player), accountId,
		/// cidPid, offerData (a stringified JSON array of every offer this message carried —
		/// byte-identical to the wire `offers` field, so all funnel stages share one shape),
		/// campaignData (free-form JSON object string), deeplink, funnelType="Sent". Empty fields
		/// are omitted to keep the payload flat.
		///
		/// Note: <c>SendAnalyticsEventBatch</c> routes the underlying POST to the request context's
		/// user (the caller). For <see cref="SendCampaignPushToSelf"/> that is the target player;
		/// for the admin <see cref="SendCampaignPushToPlayer"/> the target may differ, so the
		/// authoritative gamerTag is always carried in the event params (the path id is only
		/// routing, per §4.3). The send is fire-and-forget (the analytics service queues it).
		/// </summary>
		private void EmitSentEvent(long playerId, PushMessage message)
		{
			// §4.2 — only track campaigns that carry both ids.
			if (string.IsNullOrWhiteSpace(message.campaignId) || string.IsNullOrWhiteSpace(message.nodeId))
				return;

			var gamerTag = string.IsNullOrWhiteSpace(message.gamerTag) ? playerId.ToString() : message.gamerTag;

			var p = new Dictionary<string, object>
			{
				["campaignId"] = message.campaignId,
				["nodeId"] = message.nodeId,
				["gamerTag"] = gamerTag,
				["funnelType"] = "Sent",
			};
			// accountId is auto-set to the target player's gamerTag; callers need not send it.
			p["accountId"] = string.IsNullOrWhiteSpace(message.accountId) ? gamerTag : message.accountId;
			if (!string.IsNullOrWhiteSpace(message.cidPid)) p["cidPid"] = message.cidPid;
			if (!string.IsNullOrWhiteSpace(message.deepLink)) p["deeplink"] = message.deepLink;

			// §4.6 offerData: the offers this Sent event concerns, as a SINGLE flat column holding a
			// stringified JSON array of {itemId, value, customData} — the SAME per-offer shape the
			// native libraries emit (empties omitted, customData kept as a stringified JSON string so
			// every level stays Athena-safe). Project each offer explicitly: PushOfferData uses public
			// FIELDS, which System.Text.Json skips by default, so a bare Serialize(message.offers)
			// would emit empty objects (`[{}]`) with no itemId/value/customData.
			if (message.offers != null && message.offers.Count > 0)
			{
				var offerData = message.offers.Select(o =>
				{
					// SortedDictionary → keys serialize alphabetically (customData, itemId, value),
					// matching the native libraries (iOS encodes with .sortedKeys; Android sorts too).
					var od = new SortedDictionary<string, object>(StringComparer.Ordinal);
					if (!string.IsNullOrWhiteSpace(o.itemId)) od["itemId"] = o.itemId;
					if (!string.IsNullOrWhiteSpace(o.value)) od["value"] = o.value;
					if (!string.IsNullOrWhiteSpace(o.customData)) od["customData"] = o.customData;
					return od;
				}).ToList();
				p["offerData"] = JsonSerializer.Serialize(offerData, FunnelJson);
			}

			// Free-form campaign metadata, carried verbatim (already a JSON object string), same flat-
			// column rule as offerData. Present on every stage when the message carried it.
			if (!string.IsNullOrWhiteSpace(message.campaignData)) p["campaignData"] = message.campaignData;

			try
			{
				// Serialize a key-sorted copy so the Slack/debug payload has alphabetical top-level
				// keys — identical order to the native libraries (the analytics CoreEvent `p` itself
				// is key-addressed in Athena, so its order is irrelevant).
				var payloadJson = JsonSerializer.Serialize(
					new SortedDictionary<string, object>(p, StringComparer.Ordinal), FunnelJson);
				var ev = new CoreEvent("notification_funnel", "Sent", p);
				// DEBUG: full funnel payload as emitted (op=g.core, c=notification_funnel, e=Sent).
				BeamableLogger.Log("[funnel] emit op=g.core c=notification_funnel e=Sent player={player} payload={payload}",
					playerId, payloadJson);
				Services.Analytics.SendAnalyticsEvent(Services.Analytics.BuildRequest(ev));
				// Also forward the same analytics payload to the Slack webhook (fire-and-forget,
				// best-effort — mirrors the analytics send; never blocks or fails the push).
				_ = PostFunnelToSlack(payloadJson);
			}
			catch (Exception ex)
			{
				// Analytics is best-effort — never fail a successful push because the funnel
				// event couldn't be queued.
				BeamableLogger.LogWarning("Failed to emit 'Sent' funnel event for player {player}: {msg}", playerId, ex.Message);
			}
		}

		/// <summary>Reads the APNs credentials from this realm's config (the "apns_push" namespace).</summary>
		private async Task<ApnsSettings> LoadApnsSettings()
		{
			var config = await Services.RealmConfig.GetRealmConfigSettings();
			var ns = config.GetNamespace(ApnsSettings.Namespace);
			return ApnsSettings.FromGetter(key => ns.GetSetting(key));
		}

		/// <summary>Reads the FCM credentials from this realm's config (the "fcm_push" namespace).</summary>
		private async Task<FcmSettings> LoadFcmSettings()
		{
			var config = await Services.RealmConfig.GetRealmConfigSettings();
			var ns = config.GetNamespace(FcmSettings.Namespace);
			var settings = FcmSettings.FromGetter(key => ns.GetSetting(key));
			// Secret-free confirmation that the pasted JSON parsed (no private key in the log).
			BeamableLogger.Log("FCM config loaded from Realm Config: {summary}", settings.DescribeSafely());
			return settings;
		}

		private static string NormalizeEnvironment(string environment)
		{
			if (string.IsNullOrWhiteSpace(environment)) return null; // resolved against the realm default at send time
			var e = environment.Trim().ToLowerInvariant();
			return e is "sandbox" or "dev" or "development" ? ApnsEnvironment.Sandbox : ApnsEnvironment.Production;
		}

		/// <summary>Normalizes a platform tag; null/empty/unknown defaults to APNs for backward compatibility.</summary>
		private static string NormalizePlatform(string platform)
		{
			if (string.IsNullOrWhiteSpace(platform)) return PushPlatform.Apns;
			var p = platform.Trim().ToLowerInvariant();
			return p is "fcm" or "android" or "firebase" ? PushPlatform.Fcm : PushPlatform.Apns;
		}

		private static string Mask(string token)
		{
			if (string.IsNullOrEmpty(token)) return token;
			return token.Length <= 12 ? token : $"{token[..8]}…{token[^4..]}";
		}
	}

	/// <summary>Result of registering / unregistering a device token.</summary>
	[Serializable]
	public class RegisterResult
	{
		public bool success;
		public int deviceCount;
		public string message;
	}

	/// <summary>
	/// Result of unregistering a device. Same shape as <see cref="RegisterResult"/>,
	/// but a distinct type so the generated TypeScript client has one type per
	/// endpoint (the web-client generator emits a return type per endpoint, so two
	/// endpoints sharing one type would produce a duplicate declaration).
	/// </summary>
	[Serializable]
	public class UnregisterResult
	{
		public bool success;
		public int deviceCount;
		public string message;
	}

	/// <summary>Which push provider a device's token belongs to.</summary>
	public static class PushPlatform
	{
		public const string Apns = "apns"; // iOS
		public const string Fcm = "fcm";   // Android (and any FCM client)
	}

	/// <summary>A registered device. In responses the <see cref="token"/> is masked.</summary>
	[Serializable]
	public class DeviceInfo
	{
		public string token;
		public string platform; // "apns" (default, iOS) or "fcm" (Android). Empty/null treated as APNs.
		public string environment;
		public long updatedAt;
	}

	/// <summary>The calling player's registered devices.</summary>
	[Serializable]
	public class DeviceList
	{
		public List<DeviceInfo> devices = new();
	}

	/// <summary>Result of forwarding funnel data to the Slack webhook.</summary>
	[Serializable]
	public class WebhookResult
	{
		public bool success;
		public int statusCode;
		public string message;
	}

	/// <summary>
	/// §3.3 Notification Intent Data carried by a campaign push, plus the notification content.
	/// This is the request object for <see cref="PushNotificationService.SendCampaignPushToSelf"/>
	/// and <see cref="PushNotificationService.SendCampaignPushToPlayer"/>. All campaign fields are
	/// optional — supplying none reduces to a plain title/body/deepLink push (no funnel event).
	/// Embedded into the provider payload as the flat stringified §3.3 map; when
	/// <c>campaignId</c> + <c>nodeId</c> are present the microservice also emits a "Sent" funnel
	/// event (§4.4).
	/// </summary>
	[Serializable]
	public class PushCampaignRequest
	{
		// title/body/deepLink are the required notification content. The §3.3 campaign metadata
		// below is all optional — modeled with Beamable Optional* types so the generated web client
		// marks them optional (the schema generator unwraps Optional<T> to its inner type, so the
		// wire shape stays e.g. `campaignId?: string`, never an Optional wrapper).
		public string title;
		public string body;
		public string deepLink;              // canonical key on the wire: "deeplink"

		public OptionalString campaignId;    // NEW concept (§3.3)
		public OptionalString nodeId;        // NEW concept (§3.3)
		public OptionalString gamerTag;      // Beamable dbid; defaults to the target player id when unset
		public OptionalString accountId;     // Beamable account id
		public OptionalString cidPid;        // "<cid>.<pid>" realm scope
		public OptionalList<PushOffer> offers; // optional offers array
		public OptionalString campaignData;  // free-form JSON object, as a string

		/// <summary>
		/// Projects the schema fields (sans title/body/deepLink) into the internal context. The
		/// scalar Optional* fields convert implicitly to their inner value (null when absent); the
		/// offers DTOs are copied into their plain internal twin so no Optional reaches the device.
		/// </summary>
		public PushCampaignContext ToContext() => new PushCampaignContext
		{
			campaignId = campaignId,
			nodeId = nodeId,
			gamerTag = gamerTag,
			accountId = accountId,
			cidPid = cidPid,
			offers = ((List<PushOffer>)offers)?
				.Select(o => new PushOfferData { itemId = o.itemId, value = o.value, customData = o.customData })
				.ToList(),
			campaignData = campaignData,
		};
	}

	/// <summary>
	/// Internal carrier for the §3.3 campaign context handed to
	/// <c>DeliverToPlayer</c> (not a callable surface). Mirrors the schema fields of
	/// <see cref="PushCampaignRequest"/> minus the notification content.
	/// </summary>
	public class PushCampaignContext
	{
		public string campaignId;
		public string nodeId;
		public string gamerTag;
		public string accountId;
		public string cidPid;
		public List<PushOfferData> offers;
		public string campaignData;
	}

	/// <summary>Aggregated outcome of a send across one player's device(s).</summary>
	[Serializable]
	public class SendResult
	{
		public bool success;
		public int attempted;
		public int succeeded;
		public int failed;
		public List<string> messages = new();
	}

	/// <summary>
	/// Admin send result. Same shape as <see cref="SendResult"/> but a distinct type
	/// so the admin endpoint generates its own client return type (see the note on
	/// <see cref="UnregisterResult"/>).
	/// </summary>
	[Serializable]
	public class AdminSendResult
	{
		public bool success;
		public int attempted;
		public int succeeded;
		public int failed;
		public List<string> messages = new();
	}

	/// <summary>A player who has at least one registered device (no token is exposed).</summary>
	[Serializable]
	public class RegisteredPlayer
	{
		public long playerId;
		public int deviceCount;
		public List<string> platforms = new(); // distinct: "apns" and/or "fcm"
		public long lastUpdated;               // newest device's updatedAt (unix seconds)
		public string gamePlatform;            // THORIUM_GAME_PLATFORM (e.g. "Web"), "" if unset
		public string gameDevice;              // THORIUM_GAME_DEVICE (e.g. "Desktop"), "" if unset
	}

	/// <summary>The roster of players with registered devices, for the admin Portal tool.</summary>
	[Serializable]
	public class RegisteredPlayerList
	{
		public List<RegisteredPlayer> players = new();
		public string message; // set only when the roster couldn't be produced
	}

	/// <summary>
	/// Secret-free result of <c>CheckFcmConfig</c>. Confirms the pasted service-account JSON
	/// parsed and the private key loaded; <see cref="message"/> carries the reason on failure.
	/// The private key is never included.
	/// </summary>
	[Serializable]
	public class FcmConfigStatus
	{
		public bool configured;
		public bool privateKeyLoaded;
		public string projectId;
		public string clientEmail;
		public string tokenUri;
		public string message;
	}
}
