using Beamable.Common;
using Beamable.Server;
using System;
using System.Globalization;

namespace Beamable.LootBoxService
{
	[Microservice("LootBoxService")]
	public partial class LootBoxService : Microservice
	{
		public const int CLAIM_PERIOD_SECONDS = 10;
		public const string CLAIM_STAT = "lastClaimTime";

		[ClientCallable]
		public async Promise<double> GetTimeLeft()
		{
			var lastClaimTimeStr = await Services.Stats.GetProtectedPlayerStat(Context.UserId, CLAIM_STAT);
			var now = DateTime.UtcNow;

			if (!DateTime.TryParse(lastClaimTimeStr, out var lastClaimTime))
			{
				await Services.Stats.SetProtectedPlayerStat(Context.UserId, CLAIM_STAT,
					now.ToString(CultureInfo.InvariantCulture));
				lastClaimTime = now;
			}

			var diff = now - lastClaimTime;
			var timeLeftInSeconds = CLAIM_PERIOD_SECONDS - diff.TotalSeconds;
			timeLeftInSeconds = Math.Max(0, timeLeftInSeconds);
			return timeLeftInSeconds;
		}

		[ClientCallable]
		public async Promise<bool> Claim()
		{
			var timeLeft = await GetTimeLeft();
			if (timeLeft > 0) return false;

			await Services.Stats.SetProtectedPlayerStat(Context.UserId, CLAIM_STAT,
				DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

			await Services.Inventory.AddCurrency("currency.gems", 50);
			return true;
		}
	}
}
