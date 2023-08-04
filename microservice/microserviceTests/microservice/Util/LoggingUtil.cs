using Beamable.Common;
using Beamable.Server;
using Core.Server.Common;
using Serilog;
using Serilog.Events;
using UnityEngine;

namespace microserviceTests.microservice.Util
{
    public class LoggingUtil
    {
	    public static void InitTestCorrelator(LogEventLevel logLevel=LogEventLevel.Verbose)
        {
	        BeamableLogProvider.Provider = new BeamableSerilogProvider();
	        Debug.Instance = new MicroserviceDebug();
	        // https://github.com/serilog/serilog/wiki/Configuration-Basics
	        Log.Logger = new LoggerConfiguration()
		        .MinimumLevel.Is(logLevel)
		        .WriteTo.TestCorrelator()
		        .CreateLogger();
	        BeamableSerilogProvider.LogContext.Value = Log.Logger;
        }
    }
}
