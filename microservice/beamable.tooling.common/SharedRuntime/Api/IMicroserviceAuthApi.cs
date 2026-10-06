using System;
using System.Collections.Generic;
using System.Globalization;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Stats;
using Newtonsoft.Json;

namespace Beamable.Server.Api
{
	/// <summary>
	/// This type defines the %Microservice main entry point for the %Auth feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/identity/identity/">Identity</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>

	[RealmScoped]
	public interface IMicroserviceAuthApi : IAuthApi
	{
		/// <summary>
		/// From a user's game-specific player id, gets that user's account data. If you want the cross-game user account, see <see cref="AccountId"/> and <see cref="GetAccountId"/>.
		/// </summary>
		Promise<User> GetUser(long gamerTag);

		/// <summary>
		/// Look up many players of the current realm at once: optionally narrow them to those whose stats satisfy
		/// <see cref="BatchAccountsRequest.filter"/>, then return each remaining player's account and the stat values
		/// asked for. Any number of players may be passed; they are sent in pages of
		/// <see cref="BatchAccountsRequest.MaxPlayersPerRequest"/>.
		/// </summary>
		Promise<BatchAccountsResponse> BatchAccounts(BatchAccountsRequest request);

		/// <summary>
		/// Get the assumed user's (see <see cref="Microservice.AssumeUser"/>) cross-game account id.
		/// This is different from <see cref="RequestContext.UserId"/>, which always resolve to the user's game-specific player id.
		/// </summary>
		Promise<AccountId> GetAccountId();
	}

	/// <summary>
	/// This ID is different from <see cref="User.id"/>. In beamable, there are 2 ids:
	/// <list type="bullet">
	/// <item>A game-specific ID, which we call a player id and can be found in <see cref="User.id"/>.</item>
	/// <item>A customer-specific ID, which a single user shares across all your games/realms. This is the <see cref="AccountId"/>.</item>
	/// </list> 
	/// </summary>
	public struct AccountId
	{
		public long Id;
	}

	/// <summary>
	/// Input to <see cref="IMicroserviceAuthApi.BatchAccounts"/>: many players of the current realm, optionally narrowed
	/// by a stat filter, with the account and stat values to return for the ones that remain.
	/// </summary>
	[Serializable]
	public class BatchAccountsRequest
	{
		/// <summary>The most players the platform looks up per request; larger requests are paged.</summary>
		public const int MaxPlayersPerRequest = 500;

		/// <summary>The players to look up. Duplicates are ignored.</summary>
		public List<long> playerIds = new List<long>();

		/// <summary>
		/// When set, only players whose stats satisfy every criterion are looked up further; the rest come back in
		/// <see cref="BatchAccountsResponse.filteredOut"/>. A player with no stats in the namespace never matches.
		/// </summary>
		public BatchAccountsStatsFilter filter;

		/// <summary>Whether to return each remaining player's account.</summary>
		public bool includeAccount = true;

		/// <summary>Stat values to return for each remaining player, per namespace.</summary>
		public List<BatchAccountsStats> stats = new List<BatchAccountsStats>();
	}

	/// <summary>
	/// The stats service's search request (<c>StatsSearchRequest</c> on the platform), narrowed to what a batch lookup
	/// uses: the namespace and the criteria. The ids being looked up are always players.
	/// </summary>
	[Serializable]
	public class BatchAccountsStatsFilter
	{
		public string domain;
		public string visibility;
		public string itemType = "player";
		public List<BatchAccountsCriteria> criteria = new List<BatchAccountsCriteria>();

		public static BatchAccountsStatsFilter For(StatsDomainType domain, StatsAccessType access,
			params BatchAccountsCriteria[] criteria) => new BatchAccountsStatsFilter
		{
			domain = BatchAccountsNames.Domain(domain),
			visibility = BatchAccountsNames.Access(access),
			criteria = new List<BatchAccountsCriteria>(criteria)
		};
	}

	/// <summary>
	/// One stat comparison. <see cref="rel"/> takes the same operators as stats search: eq, neq, lt, lte, gt, gte, in,
	/// nin and nonexistent. Comparisons are type-exact, so a string stat only equals a string value.
	/// </summary>
	[Serializable]
	public class BatchAccountsCriteria
	{
		public string stat;
		public string rel;
		public object value;

		public static BatchAccountsCriteria Eq(string stat, object value) =>
			new BatchAccountsCriteria { stat = stat, rel = "eq", value = value };

		public static BatchAccountsCriteria Neq(string stat, object value) =>
			new BatchAccountsCriteria { stat = stat, rel = "neq", value = value };
	}

	[Serializable]
	public class BatchAccountsStats
	{
		public string domain;
		public string visibility;

		/// <summary>The stat keys to return. Empty returns every stat in the namespace.</summary>
		public List<string> keys = new List<string>();

		public static BatchAccountsStats For(StatsDomainType domain, StatsAccessType access, params string[] keys) =>
			new BatchAccountsStats
			{
				domain = BatchAccountsNames.Domain(domain),
				visibility = BatchAccountsNames.Access(access),
				keys = new List<string>(keys)
			};
	}

	/// <summary>
	/// Every requested player appears exactly once across <see cref="players"/>, <see cref="filteredOut"/> and
	/// <see cref="notFound"/>.
	/// </summary>
	[Serializable]
	public class BatchAccountsResponse
	{
		public List<BatchAccountsPlayer> players = new List<BatchAccountsPlayer>();

		/// <summary>Players that did not pass <see cref="BatchAccountsRequest.filter"/>.</summary>
		public List<long> filteredOut = new List<long>();

		/// <summary>
		/// Players that passed the filter but have no account in this realm. Only populated when
		/// <see cref="BatchAccountsRequest.includeAccount"/> is set.
		/// </summary>
		public List<long> notFound = new List<long>();
	}

	[Serializable]
	public class BatchAccountsPlayer
	{
		public long playerId;

		/// <summary>
		/// The player's account, as <see cref="IMicroserviceAuthApi.GetUser"/> returns it: <see cref="User.id"/> is the
		/// account id, see <see cref="playerId"/> for the player id. Null unless requested.
		/// </summary>
		public User account;

		/// <summary>
		/// Requested stat values keyed by namespace (<c>{domain}.{visibility}</c>), then stat key, in the stat's own
		/// type: a string, long, double, bool or list.
		/// </summary>
		public Dictionary<string, Dictionary<string, object>> stats = new Dictionary<string, Dictionary<string, object>>();

		/// <summary>
		/// A requested stat value as text (numbers in the invariant culture, booleans as <c>true</c>/<c>false</c>,
		/// lists as JSON), or null when the player has none.
		/// </summary>
		public string GetStat(StatsDomainType domain, StatsAccessType access, string key)
		{
			var ns = $"{BatchAccountsNames.Domain(domain)}.{BatchAccountsNames.Access(access)}";
			if (stats == null || !stats.TryGetValue(ns, out var values) || !values.TryGetValue(key, out var value) || value == null)
				return null;

			switch (value)
			{
				case string text: return text;
				case bool flag: return flag ? "true" : "false";
				case IFormattable number: return number.ToString(null, CultureInfo.InvariantCulture);
				default: return JsonConvert.SerializeObject(value);
			}
		}
	}

	internal static class BatchAccountsNames
	{
		public static string Domain(StatsDomainType domain) => domain.ToString().ToLowerInvariant();
		public static string Access(StatsAccessType access) => access.ToString().ToLowerInvariant();
	}
}
