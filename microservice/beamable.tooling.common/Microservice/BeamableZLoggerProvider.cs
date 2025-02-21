using System.Runtime.CompilerServices;
using Beamable.Common;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Beamable.Server
{

    public static class Log
    {
        public static void Debug(string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogDebug(message);
        }
        
        [Obsolete("Prefer ZLogger instead")]
        public static void Debug(string message, params object[] args)
        {
            BeamableZLoggerProvider.LogContext.Value.LogDebug(message, args);
        }

        
        public static void Error(string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogError(message);
        }
        
        [Obsolete("Prefer ZLogger instead")]
        public static void Error(string message, params object[] args)
        {
            BeamableZLoggerProvider.LogContext.Value.LogError(message, args);
        }

        
        public static void Fatal(string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogCritical(message);
        }
        public static void Fatal(Exception ex, string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogCritical(ex, message);
        }
        
        [Obsolete("Prefer ZLogger instead")]
        public static void Fatal(string message, params object[] args)
        {
            BeamableZLoggerProvider.LogContext.Value.LogCritical(message, args);
        }

        
        public static void Verbose(string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogTrace(message);
        }
        
        [Obsolete("Prefer ZLogger instead")]
        public static void Verbose(string message, params object[] args)
        {
            BeamableZLoggerProvider.LogContext.Value.LogTrace(message, args);
        }

        
        public static void Information(string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogInformation(message);
        }
        
        [Obsolete("Prefer ZLogger instead")]
        public static void Information(string message, params object[] args)
        {
            BeamableZLoggerProvider.LogContext.Value.LogInformation(message, args);
        }

        
        public static void Warning(string message)
        {
            BeamableZLoggerProvider.LogContext.Value.LogWarning(message);
        }
        
        [Obsolete("Prefer ZLogger instead")]
        public static void Warning(string message, params object[] args)
        {
            BeamableZLoggerProvider.LogContext.Value.LogWarning(message, args);
        }

        public static void Write(LogLevel level, string message)
        {
            BeamableZLoggerProvider.LogContext.Value.Log(level, message);
        }
    }
    
    public class BeamableZLoggerProvider : BeamableLogProvider
    {
        public static AsyncLocal<ILogger> LogContext = new AsyncLocal<ILogger>();

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