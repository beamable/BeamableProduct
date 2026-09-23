
using Beamable.Common;
using Microsoft.Extensions.Logging;
using ZLogger;
using System.Runtime.CompilerServices;
using Beamable.Server.Common;


namespace Beamable.Server
{


    public static class Log
    {
        public static ILogger Default => BeamableZLoggerProvider.CurrentLogger;
        public static ILogger Global => BeamableZLoggerProvider.GlobalLogger;

        public static IDisposable BeginScope(Dictionary<string, string> scope)
        {
            return Default.BeginScope(scope);
        }
        
        public static void Debug(string message)
        {
            Default.LogDebug(message);
        }
        
        public static void Debug(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Debug, message, args);
        }


        public static void Error(string message)
        {
            Default.LogError(message);
        }

        public static void Error(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Error, message, args);
        }

        public static void Error(Exception ex, string message)
        {
            Default.LogError(ex, message);
        }

        [Obsolete("Use Critical Instead")]
        public static void Fatal(string message)
        {
            Default.LogCritical(message);
        }
        [Obsolete("Use Critical Instead")]
        public static void Fatal(Exception ex, string message)
        {
            Default.LogCritical(ex, message);
        }
        [Obsolete("Use Critical Instead")]
        public static void Fatal(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Critical, message, args);
        }
        public static void Critical(string message)
        {
            Default.LogCritical(message);
        }
        public static void Critical(Exception ex, string message)
        {
            Default.LogCritical(ex, message);
        }
        public static void Critical(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Critical, message, args);
        }


        public static void Trace(string message)
        {
            Default.LogTrace(message);
        }

        public static void Trace(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Trace, message, args);
        }

        [Obsolete("Use Trace Instead")]
        public static void Verbose(string message)
        {
            Default.LogTrace(message);
        }

        [Obsolete("Use Trace Instead")]
        public static void Verbose(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Trace, message, args);
        }


        public static void Information(string message)
        {
            Default.LogInformation(message);
        }

        public static void Information(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Information, message, args);
        }

        public static void Warning(string message)
        {
            Default.LogWarning(message);
        }

        public static void Warning(string message, params object[] args)
        {
            SafeLogTemplate.Write(Default, LogLevel.Warning, message, args);
        }

        public static void Write(LogLevel level, string message)
        {
            Default.Log(level, message);
        }
        
        public static void WriteSkippingTelemetry(LogLevel level, string message)
        {
	        // The ignore attribute will be used later to filter the log record by using the attributes dictionary
	        // And DON'T send it to OTEL
	        Default.ZLog(level, $"{Constants.IGNORE_TELEMETRY_ATTRIBUTE}{message}");
        }
    }
    
    public class BeamableZLoggerProvider : BeamableLogProvider
    {
        public static AsyncLocal<ILogger> LogContext = new AsyncLocal<ILogger>();
        public static ILogger GlobalLogger;
        public static ILogger CurrentLogger => LogContext.Value ??= GlobalLogger ?? new QueuedLogger();

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
            CurrentLogger.Log(LogLevel.Information, message);
        }

        public override void Info(string message, params object[] args)
        {
            SafeLogTemplate.Write(CurrentLogger, LogLevel.Information, message, args);
        }

        public override void Warning(string message)
        {
            CurrentLogger.Log(LogLevel.Warning, message);

        }

        public override void Warning(string message, params object[] args)
        {
            SafeLogTemplate.Write(CurrentLogger, LogLevel.Warning, message, args);
        }

        public override void Error(Exception ex)
        {
            CurrentLogger.LogError(ex, ex.Message);

        }

        public override void Error(string error)
        {
            CurrentLogger.Log(LogLevel.Error, error);

        }

        public override void Error(string error, params object[] args)
        {
            SafeLogTemplate.Write(CurrentLogger, LogLevel.Error, error, args);
        }
    }
}
