using System;
using UnityEngine;

// Fault-injection watcher for the 5.1.1 BeamContext retry-buffer fix.
//
// Pre-fix, BeamContext.Try() wrote errors[attempt] into a buffer sized to
// CoreConfiguration.ContextRetryDelays.Length. With EnableInfiniteContextRetries = true,
// once `attempt` passed the array length it overflowed -> IndexOutOfRangeException.
// The fix clamps the index to the last slot.
//
// HUMAN setup (Beamable > Core Configuration asset, or the SDK settings window):
//   1. EnableInfiniteContextRetries = true
//   2. Shorten ContextRetryDelays to e.g. [1, 1] so the array is exhausted in seconds.
//   3. Induce failure: turn off networking, or point config-defaults.txt at an
//      unreachable host, so InitProcedure() keeps failing.
//   4. Attach this script to a GameObject and press Play. Let it retry well past the
//      array length (a dozen+ attempts).
//
// PASS  -> no [SMOKE] FAIL line below; retries keep cycling, error buffer stays bounded.
// FAIL  -> an IndexOutOfRangeException (or "overflow") surfaces during init.
public class SmokeRetryWatcher : MonoBehaviour
{
	void OnEnable()  => Application.logMessageReceived += OnLog;
	void OnDisable() => Application.logMessageReceived -= OnLog;

	void Start() => Debug.Log("[SMOKE] SmokeRetryWatcher armed: watching init retries for buffer overflow");

	void OnLog(string condition, string stackTrace, LogType type)
	{
		if (type != LogType.Exception && type != LogType.Error) return;
		var text = condition + "\n" + stackTrace;
		if (text.IndexOf("IndexOutOfRange", StringComparison.OrdinalIgnoreCase) >= 0 ||
		    text.IndexOf("overflow", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			Debug.LogError($"[SMOKE] FAIL retry-buffer overflow during BeamContext init: {condition}");
		}
	}
}
