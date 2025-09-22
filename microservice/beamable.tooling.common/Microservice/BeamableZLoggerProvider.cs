
using Beamable.Common;
using Microsoft.Extensions.Logging;
using ZLogger;
using System.Runtime.CompilerServices;
using Beamable.Server.Common;


namespace Beamable.Server
{


    public static class Log
    {
	    /// <summary>
	    /// The IGNORE_TELEMETRY_ATTRIBUTE is used to filter logs from telemetry
	    /// If you would like to change the name of the variable, please make sure to change on
	    /// beamable.otel.exporter.Constants.IGNORE_TELEMETRY_ATTRIBUTE as well. 
	    /// </summary>
	    public const string IGNORE_TELEMETRY_ATTRIBUTE = "";
        public static ILogger Default => BeamableZLoggerProvider.LogContext.Value;
        public static ILogger Global => BeamableZLoggerProvider.GlobalLogger;
        
        public static void Debug(string message)
        {
            Default.LogDebug(message);
        }
        
        public static void Debug(string message, params object[] args)
        {
            Default.LogDebug(message, args);
        }

        
        public static void Error(string message)
        {
            Default.LogError(message);
        }
        
        public static void Error(string message, params object[] args)
        {
            Default.LogError(message, args);
        }

        public static void Error(Exception ex, string message)
        {
            Default.LogError(ex, message);
        }

        
        public static void Fatal(string message)
        {
            Default.LogCritical(message);
        }
        public static void Fatal(Exception ex, string message)
        {
            Default.LogCritical(ex, message);
        }
        
        public static void Fatal(string message, params object[] args)
        {
            Default.LogCritical(message, args);
        }

        
        public static void Verbose(string message)
        {
            Default.LogTrace(message);
        }
        
        public static void Verbose(string message, params object[] args)
        {
            Default.LogTrace(message, args);
        }

        
        public static void Information(string message)
        {
            Default.LogInformation(message);
        }
        
        public static void Information(string message, params object[] args)
        {
            Default.LogInformation(message, args);
        }
        
        public static void Warning(string message)
        {
            Default.LogWarning(message);
        }
        
        public static void Warning(string message, params object[] args)
        {
            Default.LogWarning(message, args);
        }

        public static void Write(LogLevel level, string message)
        {
            Default.Log(level, message);
        }
        
        public static void WriteSkippingTelemetry(LogLevel level, string message)
        {
	        // The ignore attribute will be used later to filter the log record by using the attributes dictionary
	        // And DON'T send it to OTEL
	        Default.ZLog(level, $"{IGNORE_TELEMETRY_ATTRIBUTE}{message}");
        }
    }
    
    public class BeamableZLoggerProvider : BeamableLogProvider
    {
        public static AsyncLocal<ILogger> LogContext = new AsyncLocal<ILogger>();
        public static ILogger GlobalLogger;

        static BeamableZLoggerProvider()
        {
            LogContext.Value = new QueuedLogger();
        }

        public static void SetLogger(ILogger logger)
        {
            if (LogContext.Value is QueuedLogger oldQueued)
            {
                oldQueued.Flush(logger);
            }
            LogContext.Value = logger;
        }
        
        /// <summary>
        /// Gets the singleton instance of <see cref="BeamableZLoggerProvider"/>.
        /// </summary>
        public static BeamableZLoggerProvider Instance => BeamableLogProvider.Provider as BeamableZLoggerProvider;


        public override void Info(string message)
        {
            LogContext.Value.Log(LogLevel.Information, message);
        }

        public override void Info(string message, params object[] args)
        {
            LogContext.Value.Log(LogLevel.Information, message, args);

        }

        public override void Warning(string message)
        {
            LogContext.Value.Log(LogLevel.Warning, message);

        }

        public override void Warning(string message, params object[] args)
        {
            LogContext.Value.Log(LogLevel.Warning, message, args);

        }

        public override void Error(Exception ex)
        {
            LogContext.Value.LogError(ex, ex.Message);

        }

        public override void Error(string error)
        {
            LogContext.Value.Log(LogLevel.Error, error);

        }

        public override void Error(string error, params object[] args)
        {
            LogContext.Value.Log(LogLevel.Error, error, args);
        }
    }
}
