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
	private const int CheckDelay = 250; // in milliseconds

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
		Task runningMicroserviceTask = RunMicroserviceForever();

		// keep listening to changes in the portal extension app files
		Log.Information($"Path of the extension: {service.AbsolutePath}");
		var cancellationSource = new CancellationTokenSource();
		await ExtensionFileWatcher(service.AbsolutePath, cancellationSource.Token);
	}

	private async Task ExtensionFileWatcher(string path, CancellationToken token)
	{
		using var watcher = new FileSystemWatcher(path);

		watcher.Filters.Clear();
		watcher.Filters.Add("*.css");
		watcher.Filters.Add("*.svelte");
		watcher.Filters.Add("*.js");
		watcher.Filters.Add("*.html");

		watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;

		watcher.Changed += OnChanged;
		watcher.Created += OnChanged;
		watcher.Deleted += OnChanged;
		watcher.Renamed += OnChanged;

		while (!token.IsCancellationRequested)
		{
			await Task.Delay(CheckDelay, token);
		}
	}

	private static void OnChanged(object sender, FileSystemEventArgs e)
	{
		Log.Information($"Changed: {e.Name}", ConsoleColor.Cyan);
	}

	private async Task RunMicroserviceForever()
	{
		try
		{
			await BeamServer
				.Create()
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
