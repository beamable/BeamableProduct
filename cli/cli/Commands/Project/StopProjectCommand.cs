using Beamable.Common.Semantics;
using cli.Services;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using System.Net;

namespace cli.Commands.Project;

public class StopProjectCommandArgs : CommandArgs
{
	public ServiceName serviceId;
}

public class StopProjectCommandOutput
{
	public bool didStop;
}

public class StopProjectCommand : AtomicCommand<StopProjectCommandArgs, StopProjectCommandOutput>
{
	public StopProjectCommand() : base("stop", "Stop a running service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>(
				name: "service id", description: "The id of the project to stop", getDefaultValue: () => default),
			(args, i) => args.serviceId = i);
	}

	public override async Task<StopProjectCommandOutput> GetResult(StopProjectCommandArgs args)
	{
		var localServices = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols;

		HttpMicroserviceLocalProtocol service = null;
		string serviceName = args.serviceId;
		if (string.IsNullOrEmpty(args.serviceId) && localServices.Count == 1)
		{
			var onlyServiceKvp = localServices.FirstOrDefault();
			service = onlyServiceKvp.Value;
			serviceName = onlyServiceKvp.Key;
			Log.Warning($"No service id given, but because only 1 service exists, proceeding automatically with id=[{serviceName}]");
		} else if (!localServices.TryGetValue(args.serviceId, out service))
		{
			throw new CliException(
				$"The given id=[{args.serviceId}] does not match any local services in the local beamo manifest.");
		}

		
		var discovery = args.DependencyProvider.GetService<DiscoveryService>();

		ServiceDiscoveryEvent startEvt = null;
		await foreach (var evt in discovery.StartDiscovery(TimeSpan.FromSeconds(1)))
		{
			if (evt.service == serviceName && evt.isRunning)
			{
				startEvt = evt;
				break;
			}
		}
		await discovery.Stop();

		if (startEvt == null)
		{
			Log.Warning($"the {serviceName} was not running, so it could not be stopped.");
			return new StopProjectCommandOutput { didStop = false };
		}

		if (startEvt.isContainer)
		{
			Log.Warning("this command is only intended to work for locally running services in dotnet. Please use `docker stop` instead.");
			return new StopProjectCommandOutput { didStop = false };
		}

		await SendKillMessage(startEvt);
		Log.Information("stopped.");
		return new StopProjectCommandOutput { didStop = true };
	}

	async Task SendKillMessage(ServiceDiscoveryEvent evt)
	{
		using var client = new HttpClient();
		// Set up the HTTP GET request
		var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{evt.healthPort}/stop?reason=cli-request");
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
