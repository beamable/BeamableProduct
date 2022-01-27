using Beamable.Common.Leaderboards;
using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Leaderboards
{
	public interface ILeaderboardApi
	{
		UserDataCache<RankEntry> GetCache(string boardId);

		/// <summary>
		/// For partitioned or cohorted leaderboards
		/// Resolves the specific child leaderboard the player is assigned to
		/// e.g. "leaderboards.my_partitioned_board" -> "leaderboards.my_partitioned_board#0" -- where #0 denotes the partition identifier
		/// </summary>
		/// <param name="boardId">Parent Leaderboard Id</param>
		/// <param name="joinBoard">Join the board if the player is not assigned.</param>
		/// <returns></returns>
		Promise<LeaderboardAssignmentInfo> GetAssignment(string boardId, bool joinBoard);

		/// <summary>
		/// For partitioned or cohorted leaderboards
		/// Resolves the specific child leaderboard the player is assigned to
		/// Uses a cache to avoid repeat communication with the server
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="gamerTag"></param>
		/// <returns></returns>
		Promise<LeaderboardAssignmentInfo> ResolveAssignment(string boardId, long gamerTag);

		/// <summary>
		/// Get the rank of a specific player on a specific leaderboard
		/// </summary>
		/// <param name="leaderBoard"></param>
		/// <param name="gamerTag"></param>
		/// <returns></returns>
		Promise<RankEntry> GetUser(LeaderboardRef leaderBoard, long gamerTag);

		/// <summary>
		/// Get the rank of a specific player on a specific leaderboard
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="gamerTag"></param>
		/// <returns></returns>
		Promise<RankEntry> GetUser(string boardId, long gamerTag);

		/// <summary>
		/// Get a view with ranking of a specific leaderboard
		/// </summary>
		/// <param name="leaderBoard"></param>
		/// <param name="from"></param>
		/// <param name="max"></param>
		/// <param name="focus"></param>
		/// <param name="outlier"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetBoard(LeaderboardRef leaderBoard, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// Get a view with ranking of a specific leaderboard
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="from"></param>
		/// <param name="max"></param>
		/// <param name="focus"></param>
		/// <param name="outlier"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetBoard(string boardId, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// For partitioned or cohorted leaderboards
		/// Get a view with rankings of child leaderboard the current player is assigned to
		/// </summary>
		/// <param name="leaderBoard"></param>
		/// <param name="from"></param>
		/// <param name="max"></param>
		/// <param name="focus"></param>
		/// <param name="outlier"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetAssignedBoard(LeaderboardRef leaderBoard, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// For partitioned or cohorted leaderboards
		/// Get a view with rankings of child leaderboard the current player is assigned to
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="from"></param>
		/// <param name="max"></param>
		/// <param name="focus"></param>
		/// <param name="outlier"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetAssignedBoard(string boardId, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// Get a specific list of rankings by player id/gamer tag from a leaderboard
		/// </summary>
		/// <param name="leaderBoard"></param>
		/// <param name="ids"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetRanks(LeaderboardRef leaderBoard, List<long> ids);

		/// <summary>
		/// Get a specific list of rankings by player id/gamer tag from a leaderboard
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="ids"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetRanks(string boardId, List<long> ids);

		/// <summary>
		/// Replace the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="leaderBoard"></param>
		/// <param name="score"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		Promise<EmptyResponse> SetScore(LeaderboardRef leaderBoard, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Replace the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="score"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		Promise<EmptyResponse> SetScore(string boardId, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Increment (add to) the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="leaderBoard"></param>
		/// <param name="score"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		Promise<EmptyResponse> IncrementScore(LeaderboardRef leaderBoard, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Increment (add to) the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="boardId"></param>
		/// <param name="score"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		Promise<EmptyResponse> IncrementScore(string boardId, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Get the rankings of the current player's friends participating in this leaderboard
		/// </summary>
		/// <param name="leaderboard"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetFriendRanks(LeaderboardRef leaderboard);

		/// <summary>
		/// Get the rankings of the current player's friends participating in this leaderboard
		/// </summary>
		/// <param name="boardId"></param>
		/// <returns></returns>
		Promise<LeaderBoardView> GetFriendRanks(string boardId);
	}

	[Serializable]
	public class RankEntry
	{
		public long gt;
		public long rank;
		public double score;
		public RankEntryStat[] stats;

		// DEPRECATED: Do not use
		public RankEntryColumns columns;

		public string GetStat(string name)
		{
			if (stats == null)
			{
				return null;
			}

			int length = stats.Length;
			for (int i = 0; i < length; ++i)
			{
				ref var stat = ref stats[i];
				if (stat.name == name)
				{
					return stat.value;
				}
			}

			return null;
		}

		public double GetDoubleStat(string name, double fallback = 0)
		{
			var stringValue = GetStat(name);

			if (stringValue != null && double.TryParse(stringValue, out var result))
			{
				return result;
			}
			else
			{
				return fallback;
			}
		}
	}

	[Serializable]
	public class RankEntryColumns
	{
		public long score;
	}

	[Serializable]
	public struct RankEntryStat
	{
		public string name;
		public string value;
	}

	[Serializable]
	public class LeaderBoardV2ViewResponse
	{
		public LeaderBoardView lb;
	}

	[Serializable]
	public class LeaderBoardView
	{
		public long boardsize;
		public RankEntry rankgt;
		public List<RankEntry> rankings;

		public Dictionary<long, RankEntry> ToDictionary()
		{
			Dictionary<long, RankEntry> result = new Dictionary<long, RankEntry>();
			for (int i = 0; i < rankings.Count; i++)
			{
				var next = rankings[i];
				result.Add(next.gt, next);
			}

			return result;
		}

		public List<RankEntry> ToList()
		{
			List<RankEntry> result = new List<RankEntry>();

			foreach (RankEntry rankEntry in rankings)
			{
				result.Add(rankEntry);
			}

			return result;
		}
	}

	[Serializable]
	public class LeaderboardGetAssignmentRequest
	{
		public string boardId;
	}

	[Serializable]
	public class LeaderboardAssignmentInfo
	{
		public string leaderboardId;
		public long playerId;

		public LeaderboardAssignmentInfo(string leaderboardId, long playerId)
		{
			this.leaderboardId = leaderboardId;
			this.playerId = playerId;
		}
	}
}
