using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using Serilog;
using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;

namespace cli.Commands.Project;

public class StopProjectCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
}

public class StopProjectCommandOutput
{
	public string serviceName;
	public bool didStop;
}

public class StopProjectCommand : StreamCommand<StopProjectCommandArgs, StopProjectCommandOutput>
{
	public StopProjectCommand() : base("stop", "Stop a running service")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(StopProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);
		var serviceSet = new HashSet<string>(args.services);
		var discovery = args.DependencyProvider.GetService<DiscoveryService>();
		var evtTable = new Dictionary<string, ServiceDiscoveryEvent>();
		await foreach (var evt in discovery.StartDiscovery(TimeSpan.FromSeconds(1)))
		{
			if (!serviceSet.Contains(evt.service)) continue; // we don't care about this service, because we aren't stopping it.

			if (!evtTable.ContainsKey(evt.service))
			{
				evtTable[evt.service] = evt;
				if (evtTable.Keys.Count == serviceSet.Count)
				{
					// we've found all the required services...
					break;
				}
			}

		}
		await discovery.Stop();

		foreach ((string serviceName, ServiceDiscoveryEvent serviceEvt) in evtTable)
		{
			if (serviceEvt.isContainer)
			{
				Log.Warning($"Skipping service=[{serviceName}]. this command is only intended to work for locally running services in dotnet. Please use `docker stop` instead.");
				SendResults(new StopProjectCommandOutput
				{
					didStop = false,
					serviceName = serviceName
				});
				continue;
			}

			await SendKillMessage(serviceEvt);
			Log.Information($"stopped {serviceName}.");
			SendResults(new StopProjectCommandOutput
			{
				serviceName = serviceName,
				didStop = true
			});
		}
	}

	async Task SendKillMessage(ServiceDiscoveryEvent evt)
	{
		using var client = new HttpClient();
		// Set up the HTTP GET request
		var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{evt.healthPort}/stop?reason=cli-request");
		var response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			var message =
				$"Could not stop service=[{evt.service}]. Kill-Command status code=[{(int)response.StatusCode}]. {await response.Content.ReadAsStringAsync()}";
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				// because service-discovery JUST told us the service existed- if we got a 404- it is likely that the Microservice itself doesn't have the /stop endpoint, which is a 2.0 feature.
				message = "Only Beamable 2.0 Microservices can be stopped via the CLI. " + message;
			}
			throw new CliException(message, 1, true);
		}
	}
}