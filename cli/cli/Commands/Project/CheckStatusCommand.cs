using Beamable.Common.BeamCli;
using Beamable.Server;
using Beamable.Server.Common;
using cli.Services;
using cli.Utils;
using NetMQ;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.CommandLine;
// ReSharper disable InconsistentNaming

namespace cli;

public class CheckStatusCommandArgs : CommandArgs
{
}

[Serializable]
public class ServiceDiscoveryEvent
{
	public string cid, pid, prefix, service;
	public bool isRunning;
	public bool isContainer;
	public int healthPort;
	public string containerId;
}

public class CheckStatusCommand : AppCommand<CheckStatusCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, ServiceDiscoveryEvent>
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
	}

	public override async Task Handle(CheckStatusCommandArgs args)
	{
		var discovery = args.DependencyProvider.GetService<DiscoveryService>();
		await foreach (var evt in discovery.StartDiscovery())
		{
			if (!evt.isRunning)
			{
				Log.Information($"{evt.service} is off");
			}
			else
			{
				Log.Information($"{evt.service} is available prefix=[{evt.prefix}] docker=[{evt.isContainer}]");
			}
			this.SendResults(evt);
		}
	}

}
