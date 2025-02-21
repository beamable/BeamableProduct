using Microsoft.Extensions.Logging;

namespace Beamable.Server.Common;

/// <summary>
/// Utility class for handling log level parsing and conversion.
/// </summary>
public static class LogUtil
{
	public static (string text, int level) GetSeverityText(LogLevel logLevel)
	{
		// https://opentelemetry.io/docs/specs/otel/logs/data-model/#field-severitynumber
            
		//  map the string names to the commonly used Beamable phrasings.
		switch (logLevel)
		{
			case LogLevel.Trace:
				// some call it trace, we call it verbose
				return ("verbose", 1);
			case LogLevel.Debug:
				return ("debug", 5);
			case LogLevel.Information:
				return ("info", 9);
			case LogLevel.Warning:
				return ("warn", 13);
			case LogLevel.Error:
				return ("error", 17);
			case LogLevel.Critical:
				// some call it critical, we call it fatal
				return ("fatal", 21);
			default:
				return ("none", 100);
		}
	}

	
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
	
}
