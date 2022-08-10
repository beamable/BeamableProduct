using Beamable.Common;
using Serilog;

namespace cli;

public class CliSerilogProvider : BeamableLogProvider
{
	public static AsyncLocal<ILogger> LogContext = new AsyncLocal<ILogger>();

	public static CliSerilogProvider Instance => (BeamableLogProvider.Provider as CliSerilogProvider)!;

	public override void Info(string message)
	{
		LogContext.Value!.Information(message);
	}

	public override void Info(string message, params object[] args)
	{
		LogContext.Value!.Information(message, args);
	}

	public override void Warning(string message)
	{
		LogContext.Value!.Warning(message);
	}

	public override void Warning(string message, params object[] args)
	{
		LogContext.Value!.Warning(message, args);
	}

	public override void Error(Exception ex)
	{
		LogContext.Value!.Error("[Exception] {type} {message} {stacktrace}", ex?.GetType(), ex?.Message, ex?.StackTrace);
	}

	public override void Error(string error)
	{
		LogContext.Value!.Error(error);
	}

	public override void Error(string error, params object[] args)
	{
		LogContext.Value!.Error(error, args);
	}

	public void Debug(string message, params object[] args)
	{
		LogContext.Value!.Debug(message, args);
	}
	public void Debug(string message)
	{
		LogContext.Value!.Debug(message);
	}
}
