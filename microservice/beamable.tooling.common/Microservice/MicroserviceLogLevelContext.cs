using Microsoft.Extensions.Logging;

namespace Beamable.Server
{
	public static class MicroserviceLogLevelContext
	{
		public static readonly AsyncLocal<LogLevel> CurrentLogLevel = new();
	}

}
