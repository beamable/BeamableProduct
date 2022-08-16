using NewTestingTool.Constants;
using UnityEngine;

namespace NewTestingTool.Core
{
	public static class TestableDebug
	{
		public static void Log(object message)
		{
			Debug.Log($"{TestConstants.DEBUG_PREFIX} {message}");
		}

		public static void LogWarning(object message)
		{
			Debug.LogWarning($"{TestConstants.DEBUG_PREFIX} {message}");
		}

		public static void LogError(object message)
		{
			Debug.LogError($"{TestConstants.DEBUG_PREFIX} {message}");
		}

		public static string WrapWithColor(object message, Color color) =>
			$"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}>{message}</color>";
	}
}
