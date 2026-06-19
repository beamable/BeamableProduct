using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;

namespace Beamable.PushNotificationService
{
	/// <summary>
	/// A self-contained microservice that demonstrates remote push notifications
	/// through Apple's APNs, end to end:
	///
	///   1. <see cref="RegisterDeviceToken"/> — a player registers the APNs device
	///      token their app received from iOS (one token per physical device).
	///   2. <see cref="SendPushToSelf"/>       — a player sends a real remote push to
	///      their own registered device(s) — the simplest thing to demo from the app.
	///   3. <see cref="SendPushToPlayer"/>      — an admin/back-office tool sends a push
	///      to any player by id.
	///
	/// Device tokens are stored as a <b>private</b> per-player stat (see
	/// <see cref="DeviceTokenStore"/>) — no MongoDB needed for this key/value data.
	/// The actual delivery is handled by <see cref="ApnsClient"/>, which talks to
	/// Apple over HTTP/2 using token-based (JWT / .p8) authentication. The APNs
	/// credentials live in Realm Config (Portal → Realm → Config) under the
	/// "<c>apns_push</c>" namespace — see <see cref="ApnsSettings"/>.
	/// </summary>
	public partial class PushNotificationService : Microservice
	{
		// --- Registration ---------------------------------------------------

		/// <summary>
		/// Registers (or refreshes) the calling player's APNs device token. iOS hands
		/// the app a token via the native SDK's <c>tokenReceived</c> event; the app
		/// forwards it here. Safe to call repeatedly — the same token is de-duplicated
		/// and its timestamp refreshed.
		/// </summary>
		/// <param name="token">The hex APNs device token from iOS.</param>
		/// <param name="environment">"sandbox" (dev/TestFlight builds) or "production" (App Store). Defaults to the realm's configured default.</param>
		[ClientCallable]
		public async Task<RegisterResult> RegisterDeviceToken(string token, string environment)
		{
			if (string.IsNullOrWhiteSpace(token))
				return new RegisterResult { success = false, message = "A non-empty device token is required.", deviceCount = 0 };

			var env = NormalizeEnvironment(environment);
			var devices = await LoadDevices(Context.UserId);

			var existing = devices.FirstOrDefault(d => d.token == token);
			if (existing != null)
			{
				existing.environment = env;
				existing.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			}
			else
			{
				devices.Add(new DeviceInfo
				{
					token = token,
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
		/// Admin/back-office endpoint: send a remote push to a specific player by id.
		/// Only callable with an admin/developer token.
		/// </summary>
		[AdminOnlyCallable]
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

		// --- Internals ------------------------------------------------------

		private async Task<SendResult> DeliverToPlayer(long playerId, string title, string body, string deepLink)
		{
			if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
				return new SendResult { success = false, attempted = 0, succeeded = 0, failed = 0, messages = { "title or body is required." } };

			ApnsSettings settings;
			try
			{
				settings = await LoadApnsSettings();
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError("APNs config error: {msg}", ex.Message);
				return new SendResult { success = false, attempted = 0, succeeded = 0, failed = 0, messages = { $"APNs not configured: {ex.Message}" } };
			}

			var devices = await LoadDevices(playerId);
			var result = new SendResult { attempted = devices.Count };

			if (devices.Count == 0)
			{
				result.success = false;
				result.messages.Add($"Player {playerId} has no registered devices.");
				return result;
			}

			var apns = new ApnsClient();
			var message = new PushMessage { title = title, body = body, deepLink = deepLink };
			var stale = new List<string>();

			foreach (var device in devices)
			{
				var outcome = await apns.Send(settings, device, message);
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

			// APNs told us these tokens are dead (Unregistered / BadDeviceToken) —
			// prune them so we stop trying to deliver to them.
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

		private static string NormalizeEnvironment(string environment)
		{
			if (string.IsNullOrWhiteSpace(environment)) return null; // resolved against the realm default at send time
			var e = environment.Trim().ToLowerInvariant();
			return e is "sandbox" or "dev" or "development" ? ApnsEnvironment.Sandbox : ApnsEnvironment.Production;
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

	/// <summary>A registered device. In responses the <see cref="token"/> is masked.</summary>
	[Serializable]
	public class DeviceInfo
	{
		public string token;
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
}
