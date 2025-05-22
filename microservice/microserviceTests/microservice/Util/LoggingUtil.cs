using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;
using Core.Server.Common;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ZLogger;

namespace microserviceTests.microservice.Util
{
    public class LoggingUtil
    {
	    public static TestLogs testLogs;
	    public static ILogger testLogger;
	    public static ILoggerFactory testFactory;
	    
	    public static void InitTestCorrelator(LogLevel logLevel=LogLevel.Trace)
        {
	        BeamableLogProvider.Provider = new BeamableZLoggerProvider();
	        Debug.Instance = new MicroserviceDebug();
	        testLogs = new TestLogs();
	        testFactory = LoggerFactory.Create(builder =>
	        {
		        builder.SetMinimumLevel(logLevel);
		        builder.AddZLoggerLogProcessor(testLogs);
	        });
	        testLogger = testFactory.CreateLogger<TestLogs>();
	        
	        BeamableZLoggerProvider.LogContext.Value = testLogger;
        }

	    public class TestLogs : IAsyncLogProcessor
	    {
		    public List<IZLoggerEntry> allLogs = new List<IZLoggerEntry>();
		    
		    public ValueTask DisposeAsync()
		    {
			    return ValueTask.CompletedTask;
		    }

		    public void Post(IZLoggerEntry log)
		    {
			    lock (allLogs) 
			    {
				    allLogs.Add(log);
			    }
		    }
	    }
    }
    
    
}
