using Beamable.Server;
using cli.Dotnet;
using cli.Services;
using NetMQ;
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
	public string cid, pid, prefix, service;
	public bool isRunning;
	public bool isContainer;
	public string serviceType;
	public int healthPort;
	public int dataPort;
	public string containerId;
}

public class CheckStatusCommand : StreamCommand<CheckStatusCommandArgs, ServiceDiscoveryEvent>
{
	private Dictionary<string, (long, ServiceDiscoveryEntry)> _nameToEntryWithTimestamp =
		new Dictionary<string, (long, ServiceDiscoveryEntry)>();

	private InterfaceCollection _networkInterfaceCollection;

	public CheckStatusCommand() : base("ps", "List the running status of local services not running in docker")
	{
		_networkInterfaceCollection = new InterfaceCollection();
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

		await foreach (var evt in discovery.StartDiscovery(timeout))
		{
			if (!evt.isRunning)
			{
				Log.Information($"{evt.service} is off");
			}
			else
			{
				Log.Information($"{evt.service} is available prefix=[{evt.prefix}] docker=[{evt.isContainer}]");
			}
			SendResults(evt);
		}

		await discovery.Stop();
	}

}
