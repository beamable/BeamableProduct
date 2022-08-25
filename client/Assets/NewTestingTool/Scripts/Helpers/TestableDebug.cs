using UnityEngine;

using static Beamable.NewTestingTool.Constants.TestConstants.General;

namespace Beamable.NewTestingTool.Helpers.TestableDebug
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

		public static string WrapWithColor(object message, Color color) =>
			$"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}>{message}</color>";
	}
}
