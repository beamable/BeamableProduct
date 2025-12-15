using Beamable.Common.Api;
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
	private CancellationTokenSource _tokenSource;
	private string ComputedMicroserviceName;

	public PortalExtensionRunCommand() : base("run", "Runs the specified Portal Extension project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("portal-extension-name", "The name of the portal extension to run"), (arg, i) => arg.AppName = i);
	}

	public override async Task Handle(PortalExtensionRunCommandArgs args)
	{
		// Check for dependencies
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
		{
			throw new CliException("Portal Extension dependencies are missing");
		}

		// Check if service exists
		var service =
			args.BeamoLocalSystem.BeamoManifest.PortalExtensionDefinitions.FirstOrDefault(p => p.Name == args.AppName);

		if (service == null)
		{
			throw new CliException($"Couldn't find a Portal Extension service with the name: [{args.AppName}]");
		}

		// run a microservice that will be feeding portal with new builds of the portal extension app
		await RunMicroserviceForever(service.AbsolutePath, args.AppName);
	}

	private async Task RunMicroserviceForever(string fullPath, string appName)
	{
		try
		{
			//TODO put this into a background worker, show the portal URL to the extensions page
			await BeamServer
				.Create()
				.ConfigureServices((dependency) =>
				{
					var observer = new PortalExtensionObserver();
					observer.AppFilesPath = fullPath;

					// Get the file observation started
					_tokenSource = new CancellationTokenSource();
					try
					{
						var _ = observer.StartExtensionFileWatcher(_tokenSource.Token);
					}
					catch (Exception e)
					{
						throw new CliException(
							$"Portal extension file observer failed. Error: [{e.Message}] StackTrace: [{e.StackTrace}]");
					}

					dependency.AddSingleton(observer);
					Log.Information("Created the observer service");
				})
				.IncludeRoutes<PortalExtensionDiscoveryService>(routePrefix: "")
				.OverrideConfig((config) =>
				{
					config.Attributes = new DefaultMicroserviceAttributes()
					{
						MicroserviceName = GetMicroName(appName)
					};
				})
				.RunForever();
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Portal Extension Discovery service failed to run. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
	}

	private string GetMicroName(string appName)
	{
		if (!string.IsNullOrEmpty(ComputedMicroserviceName))
		{
			return ComputedMicroserviceName;
		}

		return $"__BeamPortalExtension_{appName}_{Guid.NewGuid()}";
	}

	private bool TryBuildPortalUrl(PortalExtensionRunCommandArgs args, out string portalUrl)
	{
		var cid = args.AppContext.CustomerID;
		var pid = args.AppContext.Pid;
		var microName = "micro_" + GetMicroName(args.AppName);
		var refreshToken = args.AppContext.RefreshToken;

		if (string.IsNullOrEmpty(refreshToken))
		{
			Log.Verbose("not generating portal url, because no refresh token exists.");
			portalUrl = "";
			return false;
		}

		var queryArgs = new List<string>
		{
			$"refresh_token={refreshToken}",
			$"routingKey={ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine()}"
		};
		var joinedQueryString = string.Join("&", queryArgs);
		var treatedHost = args.AppContext.Host.Replace("/socket", "")
			.Replace("wss", "https")
			.Replace("dev.", "dev-")
			.Replace("api", "portal");
		portalUrl = $"{treatedHost}/{cid}/games/{pid}/realms/{pid}/microservices/{microName}/extensions?{joinedQueryString}";

		return true;
	}
}
