using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Beamable.Server.Common;

/// <summary>
/// Utility class for handling log level parsing and conversion.
/// </summary>
public static class LogUtil
{
	/// <summary>
	/// Tries to parse a log level string and convert it to a System log event level.
	/// </summary>
	/// <param name="logLevel">The log level string to parse.</param>
	/// <param name="parsedLogLevel">The corresponding System log event level if parsing is successful.</param>
	/// <returns><c>true</c> if parsing is successful, <c>false</c> otherwise.</returns>
	public static bool TryParseSystemLogLevel(string logLevel, out LogLevel parsedLogLevel)
	{
		if (logLevel == null)
		{
			parsedLogLevel = LogLevel.Debug;
			return false;
		}

		switch (logLevel.ToLowerInvariant())
		{

			case "f":
			case "fatal":
			case "critical":
			case "c":
			case "none":
				parsedLogLevel = LogLevel.Critical;
				return true;
			case "e":
			case "err":
			case "error":
				parsedLogLevel = LogLevel.Error;
				return true;
			case "v":
			case "verbose":
			case "trace":
			case "t":
				parsedLogLevel = LogLevel.Trace;
				return true;
			case "d":
			case "dbug":
			case "dbg":
			case "debug":
				parsedLogLevel = LogLevel.Debug;
				return true;
			case "w":
			case "warn":
			case "warning":
				parsedLogLevel = LogLevel.Warning;
				return true;
			case "i":
			case "information":
			case "info":
				parsedLogLevel = LogLevel.Information;
				return true;
			default:
				parsedLogLevel = LogLevel.Debug;
				return false;
		}
	}
	
	
	/// <summary>
	/// Tries to parse a log level string and convert it to a Serilog log event level.
	/// </summary>
	/// <param name="logLevel">The log level string to parse.</param>
	/// <param name="serilogLevel">The corresponding Serilog log event level if parsing is successful.</param>
	/// <returns><c>true</c> if parsing is successful, <c>false</c> otherwise.</returns>
	public static bool TryParseLogLevel(string logLevel, out LogEventLevel serilogLevel)
	{
		if (logLevel == null)
		{
			serilogLevel = LogEventLevel.Debug;
			return false;
		}

		switch (logLevel.ToLowerInvariant())
		{

			case "f":
			case "fatal":
			case "none":
				serilogLevel = LogEventLevel.Fatal;
				return true;
			case "e":
			case "err":
			case "error":
				serilogLevel = LogEventLevel.Error;
				return true;
			case "v":
			case "verbose":
				serilogLevel = LogEventLevel.Verbose;
				return true;
			case "d":
			case "dbug":
			case "dbg":

			case "debug":
				serilogLevel = LogEventLevel.Debug;
				return true;
			case "w":
			case "warn":
			case "warning":
				serilogLevel = LogEventLevel.Warning;
				return true;
			case "i":
			case "information":
			case "info":
				serilogLevel = LogEventLevel.Information;
				return true;
			default:
				serilogLevel = LogEventLevel.Debug;
				return false;
		}
	}
}
