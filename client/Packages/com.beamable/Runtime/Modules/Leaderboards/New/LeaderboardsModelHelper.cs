using Beamable.Common.Api.Leaderboards;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Leaderboards
{
	public static class LeaderboardsModelHelper
	{
		public static RankEntry GenerateCurrentUserRankEntryTestData(string statKey, string statDefaultValue)
		{
			RankEntryStat[] stats =
			{
				new RankEntryStat
					{name = statKey, value = statDefaultValue}
			};
			
			return new RankEntry
			{
				gt = Random.Range(0, 1000000),
				rank = 1,
				score = Random.Range(0, 1000000),
				stats = stats
			};
		}
		
		public static List<RankEntry> GenerateLeaderboardsTestData(int firstId, int lastId, RankEntry currentUserEntry, string statKey, string statDefaultValue)
		{
			List<RankEntry> entries = new List<RankEntry>();

			for (int i = 0; i < lastId - firstId; i++)
			{
				int currentRank = firstId + i;

				if (currentRank == currentUserEntry.rank)
				{
					entries.Add(currentUserEntry);
				}
				else
				{
					RankEntryStat[] stats =
					{
						new RankEntryStat
							{name = statKey, value = $"{statDefaultValue} {currentRank}"}
					};

					entries.Add(new RankEntry {rank = currentRank, score = Random.Range(1, 1000000), stats = stats});
				}
			}

			return entries;
		}
	}
}
