using System.Collections.Generic;
using Beamable.Server;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{


	/*
	 * This is a sample script that can be
	 * accessed from your Unity scripts,
	 * as well as from Microservices.
	 *
	 * This script is in a common assembly
	 * definition, that is auto-referenced,
	 * and manually referenced by the Microservices.
	 *
	 * You can create your own assembly definitions,
	 * or simply use this one for any code
	 * that should be accessible from Unity
	 * and Microservice code.
	 */

	/// <summary>
	/// Defines the current high-level state of the Match Acceptance process.
	/// </summary>
	public enum MatchAcceptanceState
	{
		Accepting,
		Ready,
		PlayerDeclined,
		NotFound,
	}

	/// <summary>
	/// <see cref="StorageDocument"/> with the information/state of the existing Match Acceptance process for a given match. 
	/// </summary>
	[System.Serializable]
	public class MatchAcceptance : StorageDocument
	{

		public const string NOTIFICATION_MATCH_DECLINED = "MatchDeclined";
		public const string NOTIFICATION_MATCH_READY = "MatchReadyToStart";
		
		public string MatchId;
		public string GameTypeId;

		public long MatchEndTimeStamp;

		public long[] ExpectedPlayers;
		public bool[] AcceptedPlayers;
		public int AcceptedPlayerCount;

		public MatchAcceptanceState CurrentState;
	}

	/// <summary>
	/// <see cref="StorageDocument"/> with the information/state of the existing Match Draft process for a given match.
	/// </summary>
	[System.Serializable]
	public class MatchDraft : StorageDocument
	{
		public const string NOTIFICATION_CHARACTER_BANNED = "CharacterBanned";
		public const string NOTIFICATION_CHARACTER_SELECTED = "CharacterSelected";

		public string MatchId;
		public string GameTypeId;

		public List<long> TeamAPlayerIds;
		public List<long> TeamBPlayerIds;

		public int[] TeamALockedInCharacters;
		public int[] TeamBLockedInCharacters;

		public int[] TeamABannedCharacters;
		public int[] TeamBBannedCharacters;

		public int CurrentPickIdx; // Idx determines who needs to pick based on

		/// <summary>
		/// Helper function that gets the index in the team and the team index given any <paramref name="playerId"/>. These (<paramref name="playerIdx"/> and <paramref name="teamIdx"/>) are
		/// set to "-1" if the player is not participating in the draft.
		/// </summary>
		public void GetPlayerAndTeamIndices(long playerId, out int playerIdx, out int teamIdx)
		{
			// Get the idx into the team array for the requesting player and the team of the requesting player.
			var requesterIdxInTeamA = TeamAPlayerIds?.IndexOf(playerId) ?? -1;
			var requesterIdxInTeamB = TeamBPlayerIds?.IndexOf(playerId) ?? -1;
			if (requesterIdxInTeamA != -1)
			{
				teamIdx = 0;
				playerIdx = requesterIdxInTeamA;
			}
			else if (requesterIdxInTeamB != -1)
			{
				teamIdx = 1;
				playerIdx = requesterIdxInTeamB;
			}
			else
			{
				// Should never happen through normal client flow.
				teamIdx = -1;
				playerIdx = -1;
			}
		}
	}
}
