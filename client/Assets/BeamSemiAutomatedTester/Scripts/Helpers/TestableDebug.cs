using UnityEngine;

using static Beamable.BSAT.Constants.TestConstants.General;

namespace Beamable.BSAT
{
	public static class TestableDebug
	{
		public static void Log(object message)
		{
			Debug.Log($"{DEBUG_PREFIX} {message}");
		}

		public static void LogWarning(object message)
		{
			Debug.LogWarning($"{DEBUG_PREFIX} {message}");
		}

		public static void LogError(object message)
		{
			Debug.LogError($"{DEBUG_PREFIX} {message}");
		}
	}
}
