using Beamable.Common;
using Microsoft.Extensions.Logging;
using ZLogger;


namespace cli;

public class CliSerilogProvider : BeamableLogProvider
{
	public static AsyncLocal<ILogger> LogContext = new AsyncLocal<ILogger>();

	public static CliSerilogProvider Instance => (BeamableLogProvider.Provider as CliSerilogProvider)!;

	public override void Info(string message)
	{
		LogContext.Value!.LogInformation(message);
	}

	public override void Info(string message, params object[] args)
	{
		LogContext.Value!.LogInformation(message, args);
	}

	public override void Warning(string message)
	{
		LogContext.Value!.LogWarning(message);
	}

	public override void Warning(string message, params object[] args)
	{
		LogContext.Value!.LogWarning(message, args);
	}

	public override void Error(Exception ex)
	{
		LogContext.Value!.LogError("[Exception] {type} {message} {stacktrace}", ex?.GetType(), ex?.Message, ex?.StackTrace);
	}

	public override void Error(string error)
	{
		LogContext.Value!.LogError(error);
	}

	public override void Error(string error, params object[] args)
	{
		LogContext.Value!.LogError(error, args);
	}

	public override void Verbose(string message, params object[] args)
	{
		LogContext.Value!.LogDebug(message, args);
	}
	public override void Verbose(string message)
	{
		Console.WriteLine(message);
		LogContext.Value!.ZLogDebug($"{message}");
	}
}
