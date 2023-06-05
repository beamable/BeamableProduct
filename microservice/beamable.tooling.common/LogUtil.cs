using Serilog.Events;

namespace Beamable.Server.Common;

public static class LogUtil
{
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
