using cli.Dotnet;
using cli.Services;
using Serilog;

// ReSharper disable InconsistentNaming

namespace cli;

public class CheckStatusCommandArgs : CommandArgs
{
	public bool watch;
}

public class CheckStatusServiceResult
{
	public string service;
	public string serviceType;
	public List<string> availableRoutingKeys = new List<string>();
	public List<CheckStatusServiceInstance> runningInstances = new List<CheckStatusServiceInstance>();
}

public class CheckStatusServiceInstance
{
	public string routingKey;
	public bool isContainer;
	public int healthPort;
	public int dataPort;
	public string containerId;
	public long startedByAccountId;
	public int processId;
}

public class CheckStatusCommand : StreamCommand<CheckStatusCommandArgs, CheckStatusServiceResult>
{
	public override bool AutoLogOutput => true;

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

		TimeSpan timeout = TimeSpan.FromMilliseconds(Beamable.Common.Constants.Features.Services.DISCOVERY_RECEIVE_PERIOD_MS);
		if (args.watch)
		{
			timeout = default;
		}

		Log.Debug($"running status-check with watch=[{args.watch}] timeout=[{timeout.Milliseconds}]");


		await foreach (var state in discovery.StartDiscovery(args, timeout, args.Lifecycle.CancellationToken))
		{
			// var res = new CheckStatusServiceResult { service = state.service, serviceType = state.serviceType, };
			// foreach (var (key, instance) in state.instances)
			// {
			// 	var mappedInstance = new CheckStatusServiceInstance
			// 	{
			// 		routingKey = instance.routingKey,
			// 		startedByAccountId = instance.startedByAccountId,
			// 		processId = instance.processId,
			// 		dataPort = instance.dataPort,
			// 		healthPort = instance.healthPort,
			// 		isContainer = instance.isContainer,
			// 		containerId = instance.containerId
			// 	};
			// 	res.runningInstances.Add(mappedInstance);
			// }
			//
			// res.availableRoutingKeys = res.runningInstances.Select(x => x.routingKey).Distinct().ToList();
			//
			// Log.Information($"Service=[{res.serviceType}] has {res.runningInstances.Count} instances");
			//
			// //
			// // if (!evt.isRunning)
			// // {
			// // 	Log.Information($"{evt.service} is off");
			// // }
			// // else
			// // {
			// // 	Log.Information($"{evt.service} is available routingKeys=[{string.Join(",", evt.routingKeys)}] docker=[{evt.isContainer}]");
			// // }
			//
			// SendResults(res);
		}

		await discovery.Stop();
	}
}
