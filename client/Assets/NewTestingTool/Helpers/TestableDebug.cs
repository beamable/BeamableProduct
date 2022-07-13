using NewTestingTool;
using UnityEngine;

public static class TestableDebug
{
	public static void Log(object message)
	{
		Debug.Log($"{TestConstants.TestLabel} {message}");
	}

	public static void LogWarning(object message)
	{
		Debug.LogWarning($"{TestConstants.TestLabel} {message}");
	}

	public static void LogError(object message)
	{
		Debug.LogError($"{TestConstants.TestLabel} {message}");
	}
}
