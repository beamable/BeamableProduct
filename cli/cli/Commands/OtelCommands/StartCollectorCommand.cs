using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Server;
using System.CommandLine;

namespace cli.OtelCommands;
using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class StartCollectorCommandArgs : CommandArgs
{
	public bool Detach;
}

public class StartCollectorCommand : AppCommand<StartCollectorCommandArgs>
{
	public StartCollectorCommand() : base("start", "Starts the collector process")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--detach", () => true, "If it is false the collector process will run indefinitely"),
			(arg, b) => arg.Detach = b);
	}

	public override async Task Handle(StartCollectorCommandArgs args)
	{
		await AssertEnvironmentVars(args);
		
		var CollectorStatus = await CollectorManager.IsCollectorRunning( args.Lifecycle.CancellationToken , BeamableZLoggerProvider.LogContext.Value);

		if (!CollectorStatus.isRunning)
		{
			var basePath = CollectorManager.GetCollectorBasePathForCli();
			var status = await CollectorManager.StartCollectorAndWait(basePath, true, args.Detach, args.Lifecycle.Source, BeamableZLoggerProvider.LogContext.Value);

			Log.Information($"Collector with process id [{status.pid}] started successfully");

			
			if (!args.Detach)
			{
				await Task.Delay(-1);
			}
		}
		else
		{
			Log.Information($"The collector process is already running. Please stop the collector process and try again.");
		}
	}

	private async Promise AssertEnvironmentVars(StartCollectorCommandArgs args)
	{
		CollectorManager.AddDefaultCollectorHostAndPortFallback();
		var port = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT);
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int _))
		{
			throw new Exception("Invalid value for port");
		}

		try
		{
			var res = await args.OtelApi.GetOtelAuthWriterConfig();
			CollectorManager.AddAuthEnvironmentVars(res);
			CollectorManager.AddCollectorConfigurationToEnvironment();
		}
		catch (Exception ex)
		{
			throw new CliException(
				message: $"An error happened while trying to get otel credentials from Beamo. Message=[{ex.Message}] StackTrace=[{ex.StackTrace}]");
		}
	}
}
