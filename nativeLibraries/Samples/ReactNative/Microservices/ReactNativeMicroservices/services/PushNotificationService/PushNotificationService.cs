using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Stats;
using Beamable.Server;

namespace Beamable.PushNotificationService
{
	/// <summary>
	/// A self-contained microservice that demonstrates remote push notifications
	/// through both Apple's APNs (iOS) and Firebase Cloud Messaging (Android), end to end:
	///
	///   1. <see cref="RegisterDeviceToken"/> — a player registers the device token their
	///      app received from the OS, tagged with the platform (one token per device).
	///   2. <see cref="SendPushToSelf"/>       — a player sends a real remote push to
	///      their own registered device(s) — the simplest thing to demo from the app.
	///   3. <see cref="SendPushToPlayer"/>      — an admin/back-office tool sends a push
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

		// --- Sending --------------------------------------------------------

		/// <summary>
		/// Sends a remote push to every device the calling player has registered.
		/// The easiest end-to-end demo: register on a device, then call this from the
		/// same device. Requires a physical iOS device (APNs does not deliver to the
		/// Simulator) and valid APNs credentials in Realm Config.
		/// </summary>
		/// <param name="title">Notification title.</param>
		/// <param name="body">Notification body.</param>
		/// <param name="deepLink">Optional deep-link URL carried in the payload (the app opens it on tap).</param>
		[ClientCallable]
		public Task<SendResult> SendPushToSelf(string title, string body, string deepLink)
		{
			return DeliverToPlayer(Context.UserId, title, body, deepLink);
		}

		/// <summary>
		/// Back-office endpoint: send a remote push to a specific player by id. Exposed as
		/// <c>[ServerCallable]</c> so the Portal extension can call it — that still requires the
		/// "<c>*</c>" (admin) scope, but unlike <c>[AdminOnlyCallable]</c> it does not require a
		/// logged-in player, which a Portal extension's session does not carry.
		/// </summary>
		[ServerCallable]
		public async Task<AdminSendResult> SendPushToPlayer(long playerId, string title, string body, string deepLink)
		{
			var r = await DeliverToPlayer(playerId, title, body, deepLink);
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
		/// Portal extension to pick a recipient for <see cref="SendPushToPlayer"/>.
		///
		/// Private per-player stats aren't enumerable, so we find the roster by searching the
		/// public marker stat (<c>push_devices &gt; 0</c>) that <see cref="SaveDevices"/> keeps
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

			var response = await search.SearchStats(
				PushStatDomain, PushStatPublicAccess, PushStatPlayerType,
				new List<Criteria> { new Criteria(PublicMarkerStatKey, "gt", 0) });

			var ids = response?.ids ?? Array.Empty<long>();
			var result = new RegisteredPlayerList();

			foreach (var id in ids)
			{
				var devices = await LoadDevices(id);
				if (devices.Count == 0) continue; // marker lagged behind a prune — skip

				result.players.Add(new RegisteredPlayer
				{
					playerId = id,
					deviceCount = devices.Count,
					platforms = devices.Select(d => NormalizePlatform(d.platform)).Distinct().ToList(),
					lastUpdated = devices.Max(d => d.updatedAt),
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

		// --- Internals ------------------------------------------------------

		private async Task<SendResult> DeliverToPlayer(long playerId, string title, string body, string deepLink)
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
			var message = new PushMessage { title = title, body = body, deepLink = deepLink };
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
