using Beamable.Common.Leaderboards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Api.Leaderboards
{
	public interface ILeaderboardApi
	{
		/// <summary>
		/// Get the <see cref="UserDataCache{RankEntry}"/> with <see cref="RankEntry"/> values for some leaderboard ID.
		/// </summary>
		/// <param name="boardId">The leaderboard ID</param>
		/// <returns>A <see cref="UserDataCache{T}"/> of <see cref="RankEntry"/></returns>
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
		/// Get a view with rankings from a specific leaderboard.
		/// </summary>
		/// <param name="leaderBoard">
		/// The leaderboard from which to gather entries.
		/// </param>
		/// <param name="from">
		/// The Starting Rank you wish to have in the <see cref="LeaderBoardView"/>.
		/// Is ignore when <paramref name="focus"/> is given.
		/// </param>
		/// <param name="max">
		/// Number of ranks <paramref name="from"/>. Moves towards the lowest rank; as in, if <paramref name="from"/> is 20 and <paramref name="max"/> is 10, you'll get ranks 20~30.
		/// </param>
		/// <param name="focus">
		/// A user's GamerTag that'll be used as the middle <see cref="RankEntry"/> of the resulting <see cref="LeaderBoardView"/>.
		/// Will take <paramref name="max"/>/2 from above the focus user and <paramref name="max"/>/2 from below the focus user.
		/// It is inclusive. As in, if focus is at rank 50 and you pass in max as 10, you'll get ranks 40~61.
		/// If there are not enough entries in the leaderboard either above or below you, the corresponding '<paramref name="max"/>/2' will be truncated. 
		/// </param>
		/// <param name="outlier">
		/// A GamerTag whose rank entry is guaranteed to be included. Will be stored in <see cref="LeaderBoardView.rankgt"/>.
		/// </param>
		Promise<LeaderBoardView> GetBoard(LeaderboardRef leaderBoard, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// Get a view with ranking of a specific leaderboard
		/// </summary>
		/// <param name="boardId">
		/// The id for the leaderboard from which to gather entries.
		/// </param>
		/// <param name="from">
		/// The Starting Rank you wish to have in the <see cref="LeaderBoardView"/>.
		/// Is ignore when <paramref name="focus"/> is given.
		/// </param>
		/// <param name="max">
		/// Number of ranks <paramref name="from"/>. Moves towards the lowest rank; as in, if <paramref name="from"/> is 20 and <paramref name="max"/> is 10, you'll get ranks 20~30.
		/// </param>
		/// <param name="focus">
		/// A user's GamerTag that'll be used as the middle <see cref="RankEntry"/> of the resulting <see cref="LeaderBoardView"/>.
		/// Will take <paramref name="max"/>/2 from above the focus user and <paramref name="max"/>/2 from below the focus user.
		/// It is inclusive. As in, if focus is at rank 50 and you pass in max as 10, you'll get ranks 40~61.
		/// If there are not enough entries in the leaderboard either above or below you, the corresponding '<paramref name="max"/>/2' will be truncated. 
		/// </param>
		/// <param name="outlier">
		/// A GamerTag whose rank entry is guaranteed to be included. Will be stored in <see cref="LeaderBoardView.rankgt"/>.
		/// </param>
		Promise<LeaderBoardView> GetBoard(string boardId, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// For partitioned or cohorted leaderboards.
		/// Get a view with rankings of child leaderboard the current player is assigned to
		/// </summary>
		/// <param name="leaderBoard">
		/// The leaderboard from which to gather entries.  
		/// </param>
		/// <param name="from">
		/// The Starting Rank you wish to have in the <see cref="LeaderBoardView"/>.
		/// Is ignore when <paramref name="focus"/> is given.
		/// </param>
		/// <param name="max">
		/// Number of ranks <paramref name="from"/>. Moves towards the lowest rank; as in, if <paramref name="from"/> is 20 and <paramref name="max"/> is 10, you'll get ranks 20~30.
		/// </param>
		/// <param name="focus">
		/// A user's GamerTag that'll be used as the middle <see cref="RankEntry"/> of the resulting <see cref="LeaderBoardView"/>.
		/// Will take <paramref name="max"/>/2 from above the focus user and <paramref name="max"/>/2 from below the focus user.
		/// It is inclusive. As in, if focus is at rank 50 and you pass in max as 10, you'll get ranks 40~61.
		/// If there are not enough entries in the leaderboard either above or below you, the corresponding '<paramref name="max"/>/2' will be truncated. 
		/// </param>
		/// <param name="outlier">
		/// A GamerTag whose rank entry is guaranteed to be included. Will be stored in <see cref="LeaderBoardView.rankgt"/>.
		/// </param>
		/// <returns>
		/// A leaderboard view containing only the entries in the partition of the requesting user.
		/// </returns>
		Promise<LeaderBoardView> GetAssignedBoard(LeaderboardRef leaderBoard, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// For partitioned or cohorted leaderboards
		/// Get a view with rankings of child leaderboard the current player is assigned to
		/// </summary>
		/// <param name="boardId">
		/// The id for the leaderboard from which to gather entries.
		/// </param>
		/// <param name="from">
		/// The Starting Rank you wish to have in the <see cref="LeaderBoardView"/>.
		/// Is ignore when <paramref name="focus"/> is given.
		/// </param>
		/// <param name="max">
		/// Number of ranks <paramref name="from"/>. Moves towards the lowest rank; as in, if <paramref name="from"/> is 20 and <paramref name="max"/> is 10, you'll get ranks 20~30.
		/// </param>
		/// <param name="focus">
		/// A user's GamerTag that'll be used as the middle <see cref="RankEntry"/> of the resulting <see cref="LeaderBoardView"/>.
		/// Will take <paramref name="max"/>/2 from above the focus user and <paramref name="max"/>/2 from below the focus user.
		/// It is inclusive. As in, if focus is at rank 50 and you pass in max as 10, you'll get ranks 40~61.
		/// If there are not enough entries in the leaderboard either above or below you, the corresponding '<paramref name="max"/>/2' will be truncated. 
		/// </param>
		/// <param name="outlier">
		/// A GamerTag whose rank entry is guaranteed to be included. Will be stored in <see cref="LeaderBoardView.rankgt"/>.
		/// </param>
		/// <returns>
		/// A leaderboard view containing only the entries in the partition of the requesting user.
		/// </returns>
		Promise<LeaderBoardView> GetAssignedBoard(string boardId, int from, int max, long? focus = null, long? outlier = null);

		/// <summary>
		/// Get a specific list of rankings by player id from a leaderboard
		/// </summary>
		/// <param name="leaderBoard">
		/// The leaderboard from which to gather entries.  
		/// </param>
		/// <param name="ids">The list of GamerTags whose <see cref="RankEntry"/> we should get.</param>
		Promise<LeaderBoardView> GetRanks(LeaderboardRef leaderBoard, List<long> ids);

		/// <summary>
		/// Get a specific list of rankings by player id from a leaderboard
		/// </summary>
		/// <param name="boardId">
		/// The leaderboard from which to gather entries.  
		/// </param>
		/// <param name="ids">The list of GamerTags whose <see cref="RankEntry"/> we should get.</param>
		Promise<LeaderBoardView> GetRanks(string boardId, List<long> ids);

		/// <summary>
		/// Replace the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="stats">
		/// Arbitrary key/value pair associated with the current score entry of the requesting user.
		///
		/// Can be used to cache alias/avatar and other useful information to reduce API calls required to get the leaderboard data.
		/// Can also be used store properties that inform how the score gets updated. Ie: rate of growth in idle games.
		/// A special key exists: "_finalScore". Whatever is in this value, will be used as the score when the leaderboard gets frozen (<see cref="FreezeLeaderboard"/>).
		/// </param>
		Promise<EmptyResponse> SetScore(LeaderboardRef leaderBoard, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Replace the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="stats">
		/// Arbitrary key/value pair associated with the current score entry of the requesting user.
		///
		/// Can be used to cache alias/avatar and other useful information to reduce API calls required to get the leaderboard data.
		/// Can also be used store properties that inform how the score gets updated. Ie: rate of growth in idle games.
		/// A special key exists: "_finalScore". Whatever is in this value, will be used as the score when the leaderboard gets frozen (<see cref="FreezeLeaderboard"/>).
		/// </param>
		Promise<EmptyResponse> SetScore(string boardId, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Increment (add to) the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="stats">
		/// Arbitrary key/value pair associated with the current score entry of the requesting user.
		///
		/// Can be used to cache alias/avatar and other useful information to reduce API calls required to get the leaderboard data.
		/// Can also be used store properties that inform how the score gets updated. Ie: rate of growth in idle games.
		/// A special key exists: "_finalScore". Whatever is in this value, will be used as the score when the leaderboard gets frozen (<see cref="FreezeLeaderboard"/>).
		/// </param>
		Promise<EmptyResponse> IncrementScore(LeaderboardRef leaderBoard, double score, IDictionary<string, object> stats = null);

		/// <summary>
		/// Increment (add to) the score of the current player
		/// Cohorted and Partitioned leaderboards will automatically update the correct child leaderboard
		/// </summary>
		/// <param name="stats">
		/// Arbitrary key/value pair associated with the current score entry of the requesting user.
		///
		/// Can be used to cache alias/avatar and other useful information to reduce API calls required to get the leaderboard data.
		/// Can also be used store properties that inform how the score gets updated. Ie: rate of growth in idle games.
		/// A special key exists: "_finalScore". Whatever is in this value, will be used as the score when the leaderboard gets frozen (<see cref="FreezeLeaderboard"/>).
		/// </param>
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

		/// <summary>
		/// Freeze given leaderboard which in effect will block submitting new scores.
		/// Don't use this outside of Debug/Admin client-side code unless you are aware of the implication:
		/// A client would be able to freeze the leaderboard for all other players.
		/// </summary>
		Promise<EmptyResponse> FreezeLeaderboard(string boardId);
	}

	[Serializable]
	public class RankEntry
	{
		/// <summary>
		/// The player id of the player for this entry
		/// </summary>
		public long gt;

		/// <summary>
		/// The rank in the leaderboard for this entry
		/// </summary>
		public long rank;

		/// <summary>
		/// The score on the leaderboard for this entry
		/// </summary>
		public double score;

		/// <summary>
		/// A set of <see cref="RankEntryStat"/> values associated with this entry
		/// </summary>
		public RankEntryStat[] stats;

		/// <summary>
		/// This field will be removed in the future, please do not use.
		/// </summary>
		[Obsolete]
		public RankEntryColumns columns;

		/// <summary>
		/// Find the first stat in the <see cref="stats"/> array that matches the given <see cref="name"/> argument.
		/// </summary>
		/// <param name="name">A name of a stat</param>
		/// <returns>The string value of the found stat, or null if the stat was not found.</returns>
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

		/// <summary>
		/// Find the first stat in the <see cref="stats"/> array that matches the given <see cref="name"/> argument,
		/// and parses the string value as a double.
		/// </summary>
		/// <param name="name">The name of a stat</param>
		/// <param name="fallback">If the stat does not exist, or the value is not a parsable double, this value will be returned.</param>
		/// <returns>The parsed value of the stat. If the stat was not found, or it had a non parsable value, the <see cref="fallback"/> value will be returned.</returns>
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
		/// <summary>
		/// The name of a leaderbaord stat. This should be unique.
		/// </summary>
		public string name;

		/// <summary>
		/// The value of a leaderboard stat. This can be any string.
		/// </summary>
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
		/// <summary>
		/// The user id
		/// </summary>
		public long userId;

		/// <summary>
		/// The leaderboard id
		/// </summary>
		public string lbId;

		/// <summary>
		/// How many players the leaderboard may contain
		/// </summary>
		public long boardsize;

		/// <summary>
		/// The <see cref="RankEntry"/> of the current player
		/// </summary>
		public RankEntry rankgt
		{
			get
			{
				if (_rankgt == null || _rankgt.gt == 0)
					_rankgt = rankings?.FirstOrDefault(y => y.gt == userId);
				return _rankgt;
			}
			set => _rankgt = value;
		}

		/// <summary>
		/// Empty if no outlier was given to <see cref="ILeaderboardApi.GetBoard(Beamable.Common.Leaderboards.LeaderboardRef,int,int,System.Nullable{long},System.Nullable{long})"/>.
		/// Otherwise, this is the rank entry for the outlier.
		/// </summary>
		private RankEntry _rankgt;

		/// <summary>
		/// A set of <see cref="RankEntry"/>s that represent this section of the leaderboard view.
		/// Use the <see cref="ToDictionary"/> method to convert this list into a dictionary for more convenient access.
		/// </summary>
		public List<RankEntry> rankings;

		/// <summary>
		/// Convert the <see cref="rankings"/> list into a dictionary from player id to <see cref="RankEntry"/>.
		/// </summary>
		/// <returns>A dictionary where each key is a player id, pointing the <see cref="RankEntry"/> for that player.</returns>
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

		/// <summary>
		/// Make a copy of the <see cref="rankings"/> list
		/// </summary>
		/// <returns>A copy of the <see cref="rankings"/> list</returns>
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

	[System.Serializable]
	public class ListLeaderboardResult
	{
		/// <summary>
		/// The total number of leaderboard ids available.
		/// This is not the count of the <see cref="ids"/> list.
		/// This is the actual total number of leaderboards.
		/// </summary>
		public int total;

		/// <summary>
		/// The number of leaderboard ids that were skipped to produce the <see cref="ids"/> list.
		/// </summary>
		public int offset;

		/// <summary>
		/// A list of leaderboard ids returned in this page.
		/// </summary>
		public List<string> ids;
	}

	[Serializable]
	public class LeaderboardAssignmentInfo
	{
		/// <summary>
		/// The runtime ID of a leaderboard
		/// </summary>
		public string leaderboardId;

		/// <summary>
		/// The player id
		/// </summary>
		public long playerId;

		public LeaderboardAssignmentInfo(string leaderboardId, long playerId)
		{
			this.leaderboardId = leaderboardId;
			this.playerId = playerId;
		}
	}
}
