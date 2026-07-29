using System;
using Beamable;
using UnityEngine;

// Staged by the coding agent; driven by a human (attach to a GameObject, press Play).
// All output is prefixed [SMOKE] so the agent can grep ~/Library/Logs/Unity/Editor.log
// for results without anyone relaying the Console by hand.
//
// This is the happy-path readiness harness. It deliberately touches only the most
// stable SDK surface so it compiles against 5.1.1 without edits. Extend the marked
// section with stat / inventory / microservice assertions once your service exists
// (see SMOKE_TEST_NOTES.md "Assertion snippets").
public class SmokeRunner : MonoBehaviour
{
	async void Start()
	{
		Debug.Log("[SMOKE] SmokeRunner starting");
		try
		{
			var ctx = BeamContext.Default;
			await ctx.OnReady;
			Debug.Log($"[SMOKE] PASS BeamContext ready. cid={ctx.Cid} pid={ctx.Pid} player={ctx.PlayerId}");

			// 5.1.1 regression guard (#4694): Accounts.OnReady must resolve even when a
			// stale/invalid *remembered* device token is present. With a clean store this
			// just confirms the path is intact; see the fault-injection recipe to make it bite.
			await ctx.Accounts.OnReady;
			Debug.Log("[SMOKE] PASS Accounts.OnReady resolved");

			// ---- extend here: stats / inventory / microservice assertions ----
		}
		catch (Exception e)
		{
			Debug.LogError($"[SMOKE] FAIL init: {e.GetType().Name}: {e.Message}");
		}
	}
}
