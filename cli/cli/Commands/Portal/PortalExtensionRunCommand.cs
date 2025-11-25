using Beamable.Server;
using cli.Services.PortalExtension;
using System.CommandLine;

namespace cli.Portal;

public class PortalExtensionRunCommandArgs : CommandArgs
{
	public string AppName;
}

public class PortalExtensionRunCommand : AppCommand<PortalExtensionRunCommandArgs>
{

	public PortalExtensionRunCommand() : base("run", "Runs the specified Portal Extension project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("portal-extension-name", "The name of the portal extension to run"), (arg, i) => arg.AppName = i);
	}

	public override async Task Handle(PortalExtensionRunCommandArgs args)
	{
		// Check if service exists
		var service =
			args.BeamoLocalSystem.BeamoManifest.PortalExtensionDefinitions.FirstOrDefault(p => p.Name == args.AppName);

		if (service == null)
		{
			throw new CliException($"Couldn't find a Portal Extension service with the name: [{args.AppName}]");
		}

		// run a microservice that will be feeding portal with new builds of the portal extension app
		await RunMicroserviceForever(service.AbsolutePath);
	}

	private async Task RunMicroserviceForever(string fullPath)
	{
		try
		{
			await BeamServer
				.Create()
				.ConfigureServices((dependency) =>
				{
					var observer = new PortalExtensionObserver();
					observer.AppFilesPath = fullPath;

					dependency.AddSingleton(observer);
				})
				.IncludeRoutes<PortalExtensionDiscoveryService>(routePrefix: "")
				.RunForever();
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Portal Extension Discovery service failed to run. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
	}
}
