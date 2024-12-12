using System;
using System.Threading;
using Beamable.Common;
using Serilog;
using UnityEngine;

namespace Core.Server.Common
{
	/// <summary>
	/// BeamableSerilogProvider extends BeamableLogProvider and provides logging using Serilog.
	/// </summary>
   public class BeamableSerilogProvider : BeamableLogProvider
   {
	   /// <summary>
	   /// A per-logical-call-context logger.
	   /// </summary>
      public static AsyncLocal<ILogger> LogContext = new AsyncLocal<ILogger>();

	   /// <summary>
	   /// Gets the singleton instance of BeamableSerilogProvider.
	   /// </summary>
      public static BeamableSerilogProvider Instance => BeamableLogProvider.Provider as BeamableSerilogProvider;

	   /// <summary>
	   /// Logs an information-level message.
	   /// </summary>
      public override void Info(string message)
      {
         LogContext.Value.Information(message);
      }

	   /// <summary>
	   /// Logs an information-level message with formatted arguments.
	   /// </summary>
      public override void Info(string message, params object[] args)
      {
         LogContext.Value.Information(message, args);
      }

	   /// <summary>
	   /// Logs a warning-level message.
	   /// </summary>
	   public override void Warning(string message)
	   {
		   LogContext.Value.Warning(message);
	   }

	   /// <summary>
	   /// Logs a warning-level message with formatted arguments.
	   /// </summary>
	   public override void Warning(string message, params object[] args)
	   {
		   LogContext.Value.Warning(message, args);
	   }

	   /// <summary>
	   /// Logs an error related to an exception.
	   /// </summary>
	   public override void Error(Exception ex)
	   {
		   LogContext.Value.Error("[Exception] {type} {message} {stacktrace}", ex?.GetType(), ex?.Message, ex?.StackTrace);
	   }

	   /// <summary>
	   /// Logs an error message.
	   /// </summary>
	   public override void Error(string error)
	   {
		   LogContext.Value.Error(error);
	   }

	   /// <summary>
	   /// Logs an error message with formatted arguments.
	   /// </summary>
	   public override void Error(string error, params object[] args)
	   {
		   LogContext.Value.Error(error, args);
	   }

	   /// <summary>
	   /// Logs a debug-level message with formatted arguments.
	   /// </summary>
	   public override void Verbose(string message, params object[] args)
	   {
		   LogContext.Value.Debug(message, args);
	   }

	   /// <summary>
	   /// Logs a debug-level message.
	   /// </summary>
	   public override void Verbose(string message)
	   {
		   LogContext.Value.Debug(message);
	   }

   }
}
