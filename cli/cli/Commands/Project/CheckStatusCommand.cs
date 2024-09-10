using cli.Dotnet;
using cli.Services;
using Serilog;

// ReSharper disable InconsistentNaming

namespace cli;

public class CheckStatusCommandArgs : CommandArgs
{
	public bool watch;
}

[Serializable]
public class ServiceDiscoveryEvent
{
	/// <summary>
	/// Value has no semantic meaning when <see cref="isContainer"/> is true.
	/// Otherwise, has the OS-level process id for the running microservice task.
	/// </summary>
	public int processId;

	public string cid, pid, prefix, service;
	public bool isRunning;
	public bool isContainer;
	public string serviceType;
	public int healthPort;
	public int dataPort;
	public string executionVersion;
	public string containerId;

	/// <summary>
	/// Array of user-defined groups to which this service belongs.
	/// </summary>
	public string[] groups;

	/// <summary>
	/// List of available routing keys that can be selected for this service.
	/// </summary>
	public string[] routingKeys;
}

public class CheckStatusCommand : StreamCommand<CheckStatusCommandArgs, ServiceDiscoveryEvent>
{
	public CheckStatusCommand() : base("ps", "List the running status of local services not running in docker")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (args, i) => args.watch = i);
	}

	public override async Task Handle(CheckStatusCommandArgs args)
	{
		var discovery = args.DependencyProvider.GetService<DiscoveryService>();

		TimeSpan timeout = TimeSpan.FromMilliseconds(Beamable.Common.Constants.Features.Services.DISCOVERY_RECEIVE_PERIOD_MS * 2);
		if (args.watch)
		{
			timeout = default;
		}

		Log.Debug($"running status-check with watch=[{args.watch}] timeout=[{timeout.Milliseconds}]");


		await foreach (var evt in discovery.StartDiscovery(args, timeout, args.Lifecycle.CancellationToken))
		{
			if (!evt.isRunning)
			{
				Log.Information($"{evt.service} is off");
			}
			else
			{
				Log.Information($"{evt.service} is available routingKeys=[{string.Join(",", evt.routingKeys)}] docker=[{evt.isContainer}]");
			}

			SendResults(evt);
		}

		await discovery.Stop();
	}
}
