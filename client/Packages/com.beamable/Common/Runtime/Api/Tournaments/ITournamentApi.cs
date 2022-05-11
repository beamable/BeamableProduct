using Beamable.Common.Content;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Common.Api.Tournaments
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Tournaments feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/tournaments-feature-overview">Tournaments</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public interface ITournamentApi
	{
		Promise<TournamentInfo> GetTournamentInfo(string tournamentContentId);
		Promise<TournamentInfoResponse> GetAllTournaments(string contentId = null, int? cycle = null, bool? isRunning = null);
		Promise<TournamentChampionsResponse> GetChampions(string tournamentId, int cycleLimit = 30);

		Promise<TournamentStandingsResponse> GetGlobalStandings(string tournamentId, int cycle = -1, int from = -1,
		   int max = -1, int focus = -1);

		Promise<TournamentStandingsResponse> GetStandings(string tournamentId, int cycle = -1, int from = -1,
		   int max = -1, int focus = -1);

		Promise<TournamentStandingsResponse> GetGroupPlayers(string tournamentId, int cycle = -1, int from = -1,
			 int max = -1, int focus = -1);

		Promise<TournamentGroupsResponse> GetGroups(string tournamentId, int cycle = -1, int from = -1,
			 int max = -1, int focus = -1);

		Promise<TournamentRewardsResponse> GetUnclaimedRewards(string tournamentId);
		Promise<TournamentRewardsResponse> ClaimAllRewards(string tournamentId);
		Promise<TournamentPlayerStatus> JoinTournament(string tournamentId, double startScore = 0);
		Promise<Unit> SetScore(string tournamentId, long dbid, double score, bool incrementScore = false);
		Promise<TournamentPlayerStatusResponse> GetPlayerStatus(string tournamentId = null, string contentId = null, bool? hasUnclaimedRewards = null);
		Promise<TournamentGroupStatusResponse> GetGroupStatus(string tournamentId = null, string contentId = null);
		Promise<TournamentGroupStatusResponse> GetGroupStatuses(List<long> groupIds, string contentId);

		Promise<string> GetPlayerAlias(long playerId, string statName = "alias");
		Promise<string> GetPlayerAvatar(long playerId, string statName = "avatar");
	}


	[System.Serializable]
	public class TournamentEntry
	{
		public long playerId;
		public long rank;
		public int stageChange;
		public double score;
		public List<TournamentRewardCurrency> currencyRewards;
	}

	[System.Serializable]
	public class TournamentGroupEntry
	{
		public long groupId;
		public long rank;
		public int stageChange;
		public double score;
		public List<TournamentRewardCurrency> currencyRewards;
	}

	[System.Serializable]
	public class TournamentChampionEntry
	{
		public long playerId;
		public double score;
		public int cyclesPrior;
	}

	[System.Serializable]
	public class TournamentStandingsResponse
	{
		public TournamentEntry me;
		public List<TournamentEntry> entries;
	}

	[System.Serializable]
	public class TournamentGroupsResponse
	{
		public TournamentGroupEntry focus;
		public List<TournamentGroupEntry> entries;
	}

	[System.Serializable]
	public class TournamentChampionsResponse
	{
		public List<TournamentChampionEntry> entries;
	}

	[System.Serializable]
	public class TournamentRewardCurrency
	{
		[Tooltip(ContentObject.TooltipSymbol1)]
		public string symbol;

		[Tooltip(ContentObject.TooltipAmount1)]
		public int amount;
	}

	[System.Serializable]
	public class TournamentRewardsResponse
	{
		[Tooltip(ContentObject.TooltipTournamentRewardCurrency1)]
		public List<TournamentRewardCurrency> rewardCurrencies;
	}

	[System.Serializable]
	public class TournamentJoinRequest
	{
		[Tooltip(ContentObject.TooltipId1)]
		public string tournamentId;
	}

	[System.Serializable]
	public class TournamentScoreRequest
	{
		[Tooltip(ContentObject.TooltipId1)]
		public string tournamentId;

		[Tooltip(ContentObject.TooltipPlayerDbid1)]
		public long playerId;

		[Tooltip(ContentObject.TooltipScore1)]
		public double score;

		[Tooltip(ContentObject.TooltipIncrement1)]
		public bool increment;
		//TODO: Add optional stats to set score request
	}

	[System.Serializable]
	public class TournamentInfo
	{
		[Tooltip(ContentObject.TooltipId1)]
		public string tournamentId;

		[Tooltip(ContentObject.TooltipId1)]
		public string contentId;

		[Tooltip(ContentObject.TooltipSecondsRemaining1)]
		public long secondsRemaining;

		[Tooltip(ContentObject.TooltipCycle1)]
		public int cycle;

		[Tooltip(ContentObject.TooltipStartDate1 + ContentObject.TooltipStartDate2)]
		public string startTimeUtc;

		[Tooltip(ContentObject.TooltipEndDate1 + ContentObject.TooltipEndDate2)]
		public string endTimeUtc;
	}

	[System.Serializable]
	public class TournamentInfoResponse
	{
		[Tooltip(ContentObject.TooltipTournamentInfo1)]
		public List<TournamentInfo> tournaments;
	}

	[System.Serializable]
	public class TournamentPlayerStatus
	{
		[Tooltip(ContentObject.TooltipId1)]
		public string contentId;

		[Tooltip(ContentObject.TooltipId1)]
		public string tournamentId;

		[Tooltip(ContentObject.TooltipPlayerDbid1)]
		public long playerId;

		[Tooltip(ContentObject.TooltipTier1)]
		public int tier;

		[Tooltip(ContentObject.TooltipStage1)]
		public int stage;

		[Tooltip(ContentObject.TooltipId1)]
		public long groupId;

		[Tooltip(ContentObject.TooltipTournamentRewardCurrency1)]
		public List<TournamentRewardCurrency> unclaimedRewards;
	}

	[System.Serializable]
	public class TournamentGroupStatus
	{
		[Tooltip(ContentObject.TooltipId1)]
		public long groupId;

		[Tooltip(ContentObject.TooltipId1)]
		public string contentId;

		[Tooltip(ContentObject.TooltipId1)]
		public string tournamentId;

		[Tooltip(ContentObject.TooltipTier1)]
		public int tier;

		[Tooltip(ContentObject.TooltipStage1)]
		public int stage;
	}

	[System.Serializable]
	public class TournamentScoreResponse
	{
		[Tooltip(ContentObject.TooltipResult1)]
		public string result;
	}

	[System.Serializable]
	public class TournamentPlayerStatusResponse
	{
		[Tooltip(ContentObject.TooltipStatus)]
		public List<TournamentPlayerStatus> statuses;
	}

	[System.Serializable]
	public class TournamentGroupStatusResponse
	{
		[Tooltip(ContentObject.TooltipStatus)]
		public List<TournamentGroupStatus> statuses;
	}

	[System.Serializable]
	public class TournamentGetStatusesRequest
	{
		[Tooltip(ContentObject.TooltipId1)]
		public List<long> groupIds;

		[Tooltip(ContentObject.TooltipId1)]
		public string contentId;
	}
}
