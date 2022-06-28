using Beamable.Common;
using Beamable.Server;
using Core.Server.Common;
using Serilog;
using Serilog.Formatting.Raw;
using UnityEngine;

namespace microserviceTests.microservice.Util
{
	public class LoggingUtil
	{
		public static void Init()
		{
			BeamableLogProvider.Provider = new BeamableSerilogProvider();
			Debug.Instance = new MicroserviceDebug();
			// https://github.com/serilog/serilog/wiki/Configuration-Basics
			Log.Logger = new LoggerConfiguration()
			   .MinimumLevel.Warning()
			   .WriteTo.Console(new RawFormatter())
			   .CreateLogger();
			BeamableSerilogProvider.LogContext.Value = Log.Logger;

		}
	}
}
