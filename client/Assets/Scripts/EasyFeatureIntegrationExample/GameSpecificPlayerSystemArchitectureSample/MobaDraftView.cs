using Beamable;
using Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture;
using Beamable.EasyFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobaDraftView : MonoBehaviour, ISyncBeamableView
{
	public MobaDraftPlayerSystem CurrentRenderingSystem;
	
	public int GetEnrichOrder() => int.MaxValue - 1;

	public void EnrichWithContext(BeamContextGroup managedPlayers)
	{
		var currentContext = managedPlayers.GetSinglePlayerContext();
		CurrentRenderingSystem = currentContext.ServiceProvider.GetService<MobaDraftPlayerSystem>();
	}

	public void RenderOnGUI()
	{
		switch (CurrentRenderingSystem.CurrentDraftState)
		{
			case DraftStates.Banning:
			{
				RenderDraftHeader(CurrentRenderingSystem);
				RenderDraftBanPhase(CurrentRenderingSystem);
				break;
			}
			case DraftStates.WaitingBanEnd:
			{
				RenderDraftHeader(CurrentRenderingSystem);
				RenderDraftWaitingBans(CurrentRenderingSystem);
				break;
			}
			case DraftStates.WaitingTurnToPick:
			{
				RenderDraftHeader(CurrentRenderingSystem);
				RenderDraftWaitingPicks(CurrentRenderingSystem);
				break;
			}
			case DraftStates.Picking:
			{
				RenderDraftHeader(CurrentRenderingSystem);
				RenderTurnToPick(CurrentRenderingSystem);
				break;
			}
			case DraftStates.WaitingDraftEnd:
			{
				RenderDraftHeader(CurrentRenderingSystem);
				RenderDraftWaitingPicks(CurrentRenderingSystem);
				break;
			}
			case DraftStates.Ready:
			{
				RenderDraftHeader(CurrentRenderingSystem);
				break;
			}
		}
	}

	public void RenderDraftHeader(MobaDraftPlayerSystem mobaDraftSystem)
	{
		var draft = mobaDraftSystem.DraftState;
		var playerId = mobaDraftSystem.AuthPlayerId;

		draft.GetPlayerAndTeamIndices(playerId, out var playerIdxInTeam, out var playerTeam);
		if (playerIdxInTeam == -1 || playerTeam == -1)
			return;

		GUILayout.Label($"[Match {draft.MatchId}] Player {playerId} => Curr Pick {draft.CurrentPickIdx} | Player Team = {playerTeam} | Idx in Team {playerIdxInTeam}");

		GUILayout.Label($"Team A Bans: {string.Join(" | ", draft.TeamABannedCharacters ?? Array.Empty<int>())}");
		GUILayout.Label($"Team B Bans: {string.Join(" | ", draft.TeamBBannedCharacters ?? Array.Empty<int>())}");

		GUILayout.Label($"Team A: {string.Join(" | ", draft.TeamALockedInCharacters ?? Array.Empty<int>())}");
		GUILayout.Label($"Team B: {string.Join(" | ", draft.TeamBLockedInCharacters ?? Array.Empty<int>())}");
	}

	public void RenderDraftBanPhase(MobaDraftPlayerSystem mobaDraftSystem)
	{
		GUILayout.BeginHorizontal();
		foreach (var charId in mobaDraftSystem.SelectableCharIds)
		{
			if (GUILayout.Button($"Ban {charId}"))
				mobaDraftSystem.BanCharacter(charId);
		}

		GUILayout.EndHorizontal();
	}

	public void RenderDraftWaitingBans(MobaDraftPlayerSystem mobaDraftSystem)
	{
		var missingBans = mobaDraftSystem.GetMissingBansPlayerIds();
		var playerId = mobaDraftSystem.AuthPlayerId;
		GUILayout.Label($"[Player {playerId}] Waiting for Players [{string.Join(" | ", missingBans)}");
	}

	public void RenderDraftWaitingPicks(MobaDraftPlayerSystem mobaDraftSystem)
	{
		var draft = mobaDraftSystem.DraftState;
		var playerId = mobaDraftSystem.AuthPlayerId;

		var currTeamToPick = mobaDraftSystem.Rules.PerPickIdxTeams[draft.CurrentPickIdx];
		var currPlayerIdxToPick = mobaDraftSystem.Rules.PerPickIdxPlayerIdx[draft.CurrentPickIdx];
		GUILayout.Label($"[Player {playerId}] Waiting for Player {currPlayerIdxToPick} of Team {currTeamToPick}");
	}

	public void RenderTurnToPick(MobaDraftPlayerSystem mobaDraftSystem)
	{
		var nonBannedCharacters = mobaDraftSystem.GetPossiblePicks();
		GUILayout.Label($"[Player {mobaDraftSystem.AuthPlayerId}] Your Turn to Pick!");
		GUILayout.BeginHorizontal();
		foreach (var charId in nonBannedCharacters)
		{
			if (GUILayout.Button($"Pick {charId}"))
				mobaDraftSystem.LockInCharacter(charId);
		}

		GUILayout.EndHorizontal();
	}
}
