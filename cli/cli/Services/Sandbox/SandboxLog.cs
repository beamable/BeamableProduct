using System;
using Beamable.Server;

namespace cli.Services.Sandbox;

/// <summary>
/// Logging wrapper that survives outside an MS context. The framework's static
/// <c>Beamable.Server.Log</c> dispatches through an AsyncLocal that's only
/// populated once the microservice host has constructed its logger pipeline —
/// in unit tests, route methods are called against a bare observer instance
/// with no host, and the underlying extension methods NRE on a null logger.
///
/// This helper swallows that failure so test scenarios stay clean while
/// production runs still see the messages routed through ZLogger as normal.
/// </summary>
internal static class SandboxLog
{
	public static void Info(string message)
	{
		try { Log.Information(message); }
		catch (Exception) { /* no logger context (tests / pre-host) */ }
	}

	public static void Warn(string message)
	{
		try { Log.Warning(message); }
		catch (Exception) { /* no logger context */ }
	}

	public static void ErrorMsg(string message)
	{
		try { Log.Error(message); }
		catch (Exception) { /* no logger context */ }
	}
}
