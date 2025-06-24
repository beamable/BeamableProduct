using Beamable.Common.Semantics;
using cli.Services;
using Newtonsoft.Json;
using System.CommandLine;
using Beamable.Server;

#pragma warning disable CS0649
// ReSharper disable InconsistentNaming

namespace cli.Commands.Project;

public class TailLogsCommandArgs : CommandArgs
{
	public ServiceName service;
	public int requireProcessId;
}

public class MongoLogMessage
{
	[JsonProperty("msg")]
	public string message;
}

public class TailLogMessage
{
	[JsonProperty("__t")]
	public string timeStamp;

	[JsonProperty("__m")]
	public string message;

	[JsonProperty("__l")]
	public string logLevel;

	[JsonProperty("__raw")]
	public string raw;
}

public class TailLogMessageForClient
{
	public string raw;
	public string logLevel;
	public string message;
	public string timeStamp;

	public TailLogMessageForClient()
	{

	}

	public TailLogMessageForClient(TailLogMessage original)
	{
		logLevel = original.logLevel;
		raw = original.raw;
		timeStamp = original.timeStamp;
		message = original.message;
	}
}

public class TailLogsCommand : StreamCommand<TailLogsCommandArgs, TailLogMessageForClient>
{
	public TailLogsCommand() : base("logs", "Tail the logs of a microservice")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("service", "The name of the service to view logs for"),
			(args, i) => args.service = i);
		AddOption(new RequireProcessIdOption(), (args, i) => args.requireProcessId = i);
		AddOption(new Option<bool>("--reconnect", getDefaultValue: () => true, "If the service stops, and reconnect is enabled, then the logs command will wait for the service to restart and then reattach to logs"),
			(args, i) =>
			{
				// this is an obsolete field.
			});
	}

	public override async Task Handle(TailLogsCommandArgs args)
	{
		RequireProcessIdOption.ConfigureRequiredProcessIdWatcher(args.requireProcessId);
		
		await ProjectLogsService.Handle(args, HandleLog, args.Lifecycle.Source);
	}

	void HandleLog(TailLogMessage logMessage)
	{
		Log.Information($"[{logMessage.logLevel}] {logMessage.message}");
		SendResults(new TailLogMessageForClient(logMessage));
	}
}
