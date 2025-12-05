using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using System;
using System.Collections.Generic;

namespace Beamable.Server.Api.Stats
{
	/// <summary>
	/// This type defines the %Microservice main entry point for the %Stats feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/stats/">Stats</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IMicroserviceStatsApi : IStatsApi
	{
		/// <summary>
		/// Retrieve Stats of player using the specific domain and access type.
		/// </summary>
		/// <param name="domain">The domain which will the stats will be retrieved from, can be Game or Client</param>
		/// <param name="access">The access type of the stats, can be Private or Public</param>
		/// <param name="userId">The ID of the user that you want to retrieve the stats</param>
		/// <param name="stats">This is an optional parameter. It will filter which stats you want to retrieve instead of retrieving it all</param>
		/// <returns>A promise which contains a dictionary with the retrieved stats</returns>
		Promise<Dictionary<string, string>> GetFilteredStats(StatsDomainType domain, StatsAccessType access, long userId,
			string[] stats = null);
		
		/// <summary>
		/// Retrieve a specific Stat of player from a domain and access type.
		/// </summary>
		/// <param name="domain">The domain which will the stat will be retrieved from, can be Game or Client</param>
		/// <param name="access">The access type of the stat, can be Private or Public</param>
		/// <param name="userId">The ID of the user that you want to retrieve the stat</param>
		/// <param name="stat">Which stat name you want to retrieve</param>
		/// <returns>A promise which contains the stat value</returns>
		Promise<string> GetStat(StatsDomainType domain, StatsAccessType access, long userId, string stat);
		
		/// <summary>
		/// Set the Player Stats from a specific Domain and Access type
		/// </summary>
		/// <param name="domain">The domain which will the stats will be added/updated, can be Game or Client</param>
		/// <param name="access">The access type of the stats, can be Private or Public</param>
		/// <param name="userId">The ID of the user that you want to add/update the stats</param>
		/// <param name="stats">The Dictionary containing the key and value of the stats to be added/updated</param>
		/// <returns>Returns a Promise containing the Set Stats operation</returns>
		Promise SetStats(StatsDomainType domain, StatsAccessType access, long userId, Dictionary<string, string> stats);
		
		/// <summary>
		/// Set the Player Stats from a specific Domain and Access type
		/// </summary>
		/// <param name="domain">The domain which will the stats will be added/updated, can be Game or Client</param>
		/// <param name="access">The access type of the stats, can be Private or Public</param>
		/// <param name="userId">The ID of the user that you want to add/update the stats</param>
		/// <param name="key">The key of the stats to be added/updated</param>
		/// <param name="value">The value of the stat</param>
		/// <returns>Returns a Promise containing the Set Stats operation</returns>
		Promise SetStat(StatsDomainType domain, StatsAccessType access, long userId, string key, string value);
		
		/// <summary>
		/// Delete the Player Stats from a specific Domain and Access type
		/// </summary>
		/// <param name="domain">The domain which will the stats will be deleted, can be Game or Client</param>
		/// <param name="access">The access type of the stats, can be Private or Public</param>
		/// <param name="userId">The ID of the user that you want to delete the stats</param>
		/// <param name="stats">The Dictionary containing the key and value of the stats to be deleted</param>
		/// <returns>Returns a Promise containing the Delete Stats operation</returns>
		Promise DeleteStats(StatsDomainType domain, StatsAccessType access, long userId, string[] stats);
		
		/// <summary>
		/// Delete the Player Stat from a specific Domain and Access type
		/// </summary>
		/// <param name="domain">The domain which will the stat will be deleted, can be Game or Client</param>
		/// <param name="access">The access type of the stat, can be Private or Public</param>
		/// <param name="userId">The ID of the user that you want to delete the stat</param>
		/// <param name="key">The key of the stats to be deleted</param>
		/// <returns>Returns a Promise containing the Delete Stat operation</returns>
		Promise DeleteStat(StatsDomainType domain, StatsAccessType access, long userId, string key);
		
		/// <summary>
		/// Queries the player base for matches against specific stats defined by the given <paramref name="criteria"/>.
		/// </summary>
		/// <param name="domain">The domain to search the stat, can be Game or Client.</param>
		/// <param name="access">The access type of the stats, can be Private or Public</param>
		/// <param name="criteria">List of all <see cref="Criteria"/> that must match.</param>
		/// <returns>The list of DBIDs for all users that match ALL of the criteria provided.</returns>
		Promise<StatsSearchResponse> SearchStats(StatsDomainType domain, StatsAccessType access, List<Criteria> criteria);


		#region Obsolete methods
		
		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetStat(StatsDomainType,StatsAccessType,long,string)"/> instead.
		/// You can replace it for <code>GetStat(StatsDomainType.Game, StatsAccessType.Public, userId, stat)</code>
		/// </para>
		/// Retrieve a stat value, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="stat"></param>
		/// <returns></returns>
		/// 
		[Obsolete]
		Promise<string> GetPublicPlayerStat(long userId, string stat);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetFilteredStats"/> instead.
		/// You can replace it for <code>GetFilteredStats(StatsDomainType.Game, StatsAccessType.Public, userId, stats)</code>
		/// </para>
		/// Retrieve one or more stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<Dictionary<string, string>> GetPublicPlayerStats(long userId, string[] stats);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetStats"/> instead.
		/// You can replace it for <code>GetStats(StatsDomainType.Game, StatsAccessType.Public, userId)</code>
		/// </para>
		/// Retrieve all stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<Dictionary<string, string>> GetPublicPlayerStats(long userId);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetStat(StatsDomainType,StatsAccessType,long,string)"/> instead.
		/// You can replace it for <code>GetStat(StatsDomainType.Game, StatsAccessType.Private, userId, key)</code>
		/// </para>
		/// Retrieve a stat value, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<string> GetProtectedPlayerStat(long userId, string key);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetFilteredStats"/> instead.
		/// You can replace it for <code>GetStat(StatsDomainType.Game, StatsAccessType.Private, userId, stats)</code>
		/// </para>
		/// Retrieve one or more stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<Dictionary<string, string>> GetProtectedPlayerStats(long userId, string[] stats);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetStat"/> instead.
		/// You can replace it for <code>GetStat(StatsDomainType.Game, StatsAccessType.Private, userId)</code>
		/// </para>
		/// Retrieve all stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<Dictionary<string, string>> GetProtectedPlayerStats(long userId);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="GetStats"/> instead.
		/// You can replace it for <code>GetStats(StatsDomainType.Game, StatsAccessType.Private, userId)</code>
		/// </para>
		/// Retrieve all stat values, each by key
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<Dictionary<string, string>> GetAllProtectedPlayerStats(long userId);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="SetStat(StatsDomainType,StatsAccessType,long,string,string)"/> instead.
		/// You can replace it for <code>SetStats(StatsDomainType.Game, StatsAccessType.Private, userId, key, value)</code>
		/// </para>
		/// Set a stat value, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<EmptyResponse> SetProtectedPlayerStat(long userId, string key, string value);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="SetStats(StatsDomainType,StatsAccessType,long,Dictionary{string,string})"/> instead.
		/// You can replace it for <code>SetStats(StatsDomainType.Game, StatsAccessType.Private, userId, stats)</code>
		/// </para>
		/// Set one or more stat values, by key
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		[Obsolete]
		Promise<EmptyResponse> SetProtectedPlayerStats(long userId, Dictionary<string, string> stats);

		[Obsolete("This method is obsolete, please use SetStats(StatsDomainType domain, StatsAccessType access, long userId, Dictionary<string, string> stats) instead.")]
		Promise<EmptyResponse> SetStats(string domain, string access, string type, long userId,
		   Dictionary<string, string> stats);
		
		[Obsolete("This method is obsolete, please use GetStats(StatsDomainType domain, StatsAccessType access, long userId, string[] stats = null) instead.")]
		Promise<Dictionary<string, string>> GetStats(string domain, string access, string type, long userId,
		   string[] stats);

		/// <summary>
		/// Queries the player base for matches against specific stats defined by the given <paramref name="criteria"/>.
		/// </summary>
		/// <param name="domain">"game" or "player".</param>
		/// <param name="access">"public" or "private"</param>
		/// <param name="type">Should always be "player" (exists for legacy reasons).</param>
		/// <param name="criteria">List of all <see cref="Criteria"/> that must match.</param>
		/// <returns>The list of DBIDs for all users that match ALL of the criteria provided.</returns>
		[Obsolete("This method is obsolete, please use SearchStats(StatsDomainType domain, StatsAccessType access, List<Criteria> criteria) instead")]
		Promise<StatsSearchResponse> SearchStats(string domain, string access, string type, List<Criteria> criteria);

		/// <summary>
		/// <para>
		/// This method is obsolete, please use <see cref="DeleteStats(StatsDomainType,StatsAccessType,long,string[])"/> instead.
		/// You can replace it for <code>DeleteStats(StatsDomainType.Game, StatsAccessType.Private, userId, stats)</code>
		/// </para>
		/// Deletes a player's game private stats.
		/// </summary>
		/// <param name="userId">A player's realm-specific Player id (for example, <see cref="RequestContext.UserId"/>).</param>
		/// <param name="stats">The list of stats to delete.</param>
		/// <returns></returns>
		[Obsolete]
		Promise DeleteProtectedPlayerStats(long userId, string[] stats);

		/// <summary>
		/// Deletes the given stats.
		/// </summary>
		/// <param name="domain">"game" or "player".</param>
		/// <param name="access">"public" or "private"</param>
		/// <param name="type">Should always be "player" (exists for legacy reasons).</param>
		/// <param name="userId">A player's realm-specific Player id (for example, <see cref="RequestContext.UserId"/>).</param>
		/// <param name="stats">The list of stats to delete.</param>
		[Obsolete("This method is obsolete, please use DeleteStats(StatsDomainType domain, StatsAccessType access, long userId, string[] stats) instead")]
		Promise DeleteStats(string domain, string access, string type, long userId, string[] stats);
		
		#endregion
	}
}
