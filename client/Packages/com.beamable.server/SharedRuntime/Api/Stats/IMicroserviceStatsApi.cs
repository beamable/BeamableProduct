using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using System.Collections.Generic;

namespace Beamable.Server.Api.Stats
{
	/// <summary>
	/// This type defines the %Microservice main entry point for the %Stats feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/stats-feature">Stats</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IMicroserviceStatsApi : IStatsApi
	{
		/// <summary>
		/// Retrieve a stat value, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		Promise<string> GetProtectedPlayerStat(long userId, string key);

		/// <summary>
		/// Retrieve one or more stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		Promise<Dictionary<string, string>> GetProtectedPlayerStats(long userId, string[] stats);

		/// <summary>
		/// Retrieve all stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		Promise<Dictionary<string, string>> GetAllProtectedPlayerStats(long userId);

		/// <summary>
		/// Set a stat value, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Promise<EmptyResponse> SetProtectedPlayerStat(long userId, string key, string value);

		/// <summary>
		/// Set one or more stat values, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		Promise<EmptyResponse> SetProtectedPlayerStats(long userId, Dictionary<string, string> stats);

		Promise<EmptyResponse> SetStats(string domain,
										string access,
										string type,
										long userId,
										Dictionary<string, string> stats);

		Promise<Dictionary<string, string>> GetStats(string domain,
													 string access,
													 string type,
													 long userId,
													 string[] stats);

		Promise<StatsSearchResponse> SearchStats(string domain, string access, string type, List<Criteria> criteria);
	}
}
