using Beamable;
using Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture;
using Beamable.EasyFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobaDraftAcceptanceView : MonoBehaviour, ISyncBeamableView
{
	private MobaDraftPlayerSystem ActiveRenderingSystem;

	public int GetEnrichOrder() => int.MaxValue;

	public void EnrichWithContext(BeamContextGroup managedPlayers)
	{
		// Because this is a highly specific system, you don't need to care about making any IBeamableViewDeps to hide the system behind it -- it also doesn't make that much sense to unit test this view.
		// Just depend directly on the system --- these are tightly-coupled by design. Whenever this happens, just take the shortest path to the feature.
		var currentContext = managedPlayers.GetSinglePlayerContext();
		ActiveRenderingSystem = currentContext.ServiceProvider.GetService<MobaDraftPlayerSystem>();
	}

	public void RenderOnGUI()
	{
		switch (ActiveRenderingSystem.CurrentDraftState)
		{
			case DraftStates.AcceptDecline:
				RenderAcceptDecline(ActiveRenderingSystem);
				break;
			case DraftStates.WaitingForAcceptance:
				RenderWaitingForAcceptance(ActiveRenderingSystem);
				break;
			case DraftStates.Declined:
				RenderDeclined(ActiveRenderingSystem);
				break;
		}
	}

	public void RenderAcceptDecline(MobaDraftPlayerSystem draftPlayerSystem)
	{
		var matchAcceptance = draftPlayerSystem.AcceptanceState;
		var playerId = draftPlayerSystem.AuthPlayerId;
		GUILayout.Label($"[Match {matchAcceptance.MatchId}] Player {playerId} => Num Accepted Players {matchAcceptance.AcceptedPlayerCount} | State {matchAcceptance.CurrentState}");
		if (GUILayout.Button($"Accept")) draftPlayerSystem.AcceptMatch();
		if (GUILayout.Button("Decline")) draftPlayerSystem.DeclineMatch();
	}

	public void RenderWaitingForAcceptance(MobaDraftPlayerSystem draftPlayerSystem)
	{
		var matchAcceptance = draftPlayerSystem.AcceptanceState;
		var playerId = draftPlayerSystem.AuthPlayerId;
		GUILayout.Label(
			$"[Match {matchAcceptance.MatchId}] Player {playerId} => Accepted!!! Num Accepted Players {matchAcceptance.AcceptedPlayerCount} | State {matchAcceptance.CurrentState}");
	}
	
	public void RenderDeclined(MobaDraftPlayerSystem matchStartSystemV2)
	{
		if (GUILayout.Button("A player declined! Click to Return To Queue"))
		{
			matchStartSystemV2.InvalidateMatchStart();
		}
	}
}
