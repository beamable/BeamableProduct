using Beamable.Common.Semantics;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;

#pragma warning disable CS0649
// ReSharper disable InconsistentNaming

namespace cli.Commands.Project;

public class TailLogsCommandArgs : CommandArgs
{
	public bool reconnect;
	public ServiceName service;
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
		AddOption(new Option<bool>("--reconnect", getDefaultValue: () => true, "If the service stops, and reconnect is enabled, then the logs command will wait for the service to restart and then reattach to logs"), (args, i) => args.reconnect = i);
	}

	public override async Task Handle(TailLogsCommandArgs args)
	{
		await ProjectLogsService.Handle(args, HandleLog);
	}

	void HandleLog(string logMessage)
	{
		var parsed = JsonConvert.DeserializeObject<TailLogMessage>(logMessage);
		Log.Information($"[{parsed.logLevel}] {parsed.message}");
		parsed.raw = logMessage;
		SendResults(new TailLogMessageForClient(parsed));
	}
}
