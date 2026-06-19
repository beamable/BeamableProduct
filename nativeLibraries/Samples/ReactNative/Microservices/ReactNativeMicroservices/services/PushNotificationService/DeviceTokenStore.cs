using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Stats;
using Beamable.Server;

namespace Beamable.PushNotificationService
{
	/// <summary>
	/// Persistence for a player's APNs device tokens. We store them as a single
	/// <b>private</b> per-player stat holding a JSON array — Stats is built into
	/// every realm (no MongoDB to provision) and a player only ever has a handful
	/// of devices, so a JSON blob is plenty. Because the microservice runs with a
	/// privileged identity it can read/write any player's stats, which is what
	/// lets an admin send to an arbitrary <c>playerId</c>.
	///
	/// These are <c>partial</c> methods on the service so <c>Services</c> stays in
	/// scope — that avoids hard-coding the Stats API interface type name.
	/// </summary>
	public partial class PushNotificationService
	{
		/// <summary>Stat domain/access for the stored token list.</summary>
		private const string DeviceStatKey = "apns_devices";

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			IncludeFields = true, // DeviceInfo uses public fields, not properties
		};

		/// <summary>Loads the device list for a player (empty list if none registered).</summary>
		private async Task<List<DeviceInfo>> LoadDevices(long playerId)
		{
			var raw = await Services.Stats.GetStat(
				StatsDomainType.Game, StatsAccessType.Private, playerId, DeviceStatKey);

			if (string.IsNullOrWhiteSpace(raw)) return new List<DeviceInfo>();

			try
			{
				return JsonSerializer.Deserialize<List<DeviceInfo>>(raw, JsonOptions) ?? new List<DeviceInfo>();
			}
			catch (System.Exception ex)
			{
				BeamableLogger.LogError("Failed to parse stored devices for {player}: {msg}", playerId, ex.Message);
				return new List<DeviceInfo>();
			}
		}

		/// <summary>Persists the device list for a player.</summary>
		private async Task SaveDevices(long playerId, List<DeviceInfo> devices)
		{
			var json = JsonSerializer.Serialize(devices, JsonOptions);
			await Services.Stats.SetStat(
				StatsDomainType.Game, StatsAccessType.Private, playerId, DeviceStatKey, json);
		}
	}
}
