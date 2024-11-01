using cli.Dotnet;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using System.Net;

namespace cli.Commands.Project;

public class StopProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public bool taskKill;
}

public class StopProjectCommandOutput
{
	public string serviceName;
	public ServiceInstance instance;
}

public class StopProjectCommand : StreamCommand<StopProjectCommandArgs, StopProjectCommandOutput>
{
	public StopProjectCommand() : base("stop", "Stop a running service")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);

		AddOption(
			new Option<bool>(new string[] { "--kill-task", "-k" },
				() => false,
				"Kill the task instead of sending a graceful shutdown signal via the socket"),
			(args, b) => args.taskKill = b);
	}

	public override async Task Handle(StopProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		await DiscoverAndStopServices(args, new HashSet<string>(args.services), args.taskKill, TimeSpan.FromSeconds(1), SendResults);
	}

	public static async Task DiscoverAndStopServices(CommandArgs args, HashSet<string> serviceIds, bool kill, TimeSpan discoveryPeriod, Action<StopProjectCommandOutput> onStopCallback, Func<ServiceInstance, bool> filter = null)
	{
		if (filter == null)
		{
			filter = (_) => true;
		}

		var stoppedInstances = new List<ServiceInstance>();
		await foreach (var status in CheckStatusCommand.CheckStatus(args, discoveryPeriod, DiscoveryMode.LOCAL, token: args.Lifecycle.CancellationToken))
		{
			foreach (var service in status.services)
			{
				if (!serviceIds.Contains(service.service)) continue;

				foreach (var routable in service.availableRoutes)
				{
					foreach (var instance in routable.instances)
					{
						if (!filter(instance)) continue;
						await StopRunningService(instance, args.BeamoLocalSystem, service.service, kill: kill, evt =>
						{
							onStopCallback?.Invoke(new StopProjectCommandOutput { instance = instance, serviceName = service.service, });
							stoppedInstances.Add(instance);
						});
					}
				}
			}
		}

		Log.Information($"Stopped {stoppedInstances.Count} instances.");
	}

	public static async Task StopRunningService(ServiceInstance instance, BeamoLocalSystem beamoLocalSystem, string serviceName, bool kill, Action<ServiceInstance> onServiceStopped = null)
	{
		Log.Debug($"stopping service=[{serviceName}]");
		if (instance.latestDockerEvent is not null)
		{
			await beamoLocalSystem.SynchronizeInstanceStatusWithDocker(beamoLocalSystem.BeamoManifest, beamoLocalSystem.BeamoRuntime.ExistingLocalServiceInstances);
			await beamoLocalSystem.StartListeningToDocker();

			await ServicesResetContainerCommand.TurnOffContainers(beamoLocalSystem, new[] { serviceName }, _ =>
			{
				onServiceStopped?.Invoke(instance);
			});
		}
		else if (instance.latestHostEvent is { } hostEvt)
		{
			if (kill)
			{
				try
				{
					var proc = Process.GetProcessById(hostEvt.processId);
					proc.Kill();
				}
				catch (Exception ex)
				{
					Log.Verbose($"killing process=[{hostEvt.processId}] results in error=[{ex.Message}]");
				}
			}
			else
			{
				await SendKillMessage(serviceName, hostEvt);
			}
			Log.Information($"stopped {serviceName}.");
			onServiceStopped?.Invoke(instance);
		}
	}

	public static async Task SendKillMessage(string service, HostServiceDescriptor host)
	{
		using var client = new HttpClient();
		// Set up the HTTP GET request
		Log.Verbose($"Sending kill event, host=[{JsonConvert.SerializeObject(host, Formatting.Indented)}]");
		var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{host.healthPort}/stop?reason=cli-request");
		var response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			var message =
				$"Could not stop service=[{service}]. Kill-Command status code=[{(int)response.StatusCode}]. {await response.Content.ReadAsStringAsync()}";
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				// because service-discovery JUST told us the service existed- if we got a 404- it is likely that the Microservice itself doesn't have the /stop endpoint, which is a 2.0 feature.
				message = "Only Beamable 2.0 Microservices can be stopped via the CLI. " + message;
			}

			throw new CliException(message, 1, true);
		}
	}
}
