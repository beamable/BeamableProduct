using cli.Services;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesListCommandArgs : LoginCommandArgs
{
	public bool Remote;
	public bool AsJson;
}

public class ServicesListCommand : AppCommand<ServicesListCommandArgs>
{
	private IAppContext _ctx;
	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;

	public ServicesListCommand() :
		base("ps",
			"Lists the current local or remote service manifest and status (as summary table or json).")
	{

	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--remote", "Makes it so that we output the current realm's remote manifest, instead of the local one."),
			(args, i) => args.Remote = i);

		AddOption(new Option<bool>("--json", "Outputs as json instead of summary table."),
			(args, i) => args.AsJson = i);
	}

	public override async Task Handle(ServicesListCommandArgs args)
	{
		_ctx = args.AppContext;
		_localBeamo = args.BeamoLocalSystem;
		_remoteBeamo = args.BeamoService;
		// //await _remoteBeamo.GetMetricsUrl("test", "cpu");
		// var templates = await _remoteBeamo.Promote(_ctx.Pid);
		// Console.WriteLine($"{string.Join("", JsonConvert.SerializeObject(templates))}");

		var titleText = !args.Remote ? "Local Services Status" : "Remote Services Status";
		AnsiConsole.MarkupLine($"[lightskyblue1]{titleText}[/]");

		// Declare style for column headers
		var columnNameStyle = new Style(Color.SlateBlue1);

		if (args.Remote)
		{
			var response = await AnsiConsole.Status()
				.Spinner(Spinner.Known.Default)
				.StartAsync("Sending Request...", async ctx =>
					(await _remoteBeamo.GetCurrentManifest(), await _remoteBeamo.GetStatus())
				);

			(var manifest, var status) = response;

			// Update the local manifest given the remote one.
			await _localBeamo.SyncLocalManifestWithRemote(manifest);
			_localBeamo.SaveBeamoLocalManifest();

			if (!args.AsJson)
			{
				var table = new Table();
				var beamoIdColumn = new TableColumn(new Markup("Beam-O Id", columnNameStyle));
				var imageNameColumn = new TableColumn(new Markup("Image Id", columnNameStyle));
				var shouldBeRunningColumn = new TableColumn(new Markup("Should be Running", columnNameStyle));
				var isRunningColumn = new TableColumn(new Markup("Is Running", columnNameStyle));
				table.AddColumn(beamoIdColumn).AddColumn(imageNameColumn).AddColumn(shouldBeRunningColumn).AddColumn(isRunningColumn);

				foreach (var responseService in manifest.manifest)
				{
					var beamoId = new Markup(responseService.serviceName);
					var imageId = new Markup(responseService.imageId);
					var remoteTargetStatus = new Markup(responseService.enabled ? "[green]Should be Enabled[/]" : "[red]Should be Disabled[/]");
					var remoteStatus = new Markup(status.services.First(s => s.serviceName == responseService.serviceName).running ? "[green]On[/]" : "[red]Off[/]");
					table.AddRow(new TableRow(new[] { beamoId, imageId, remoteTargetStatus, remoteStatus }));
				}

				AnsiConsole.Write(table);
			}
			else
			{
				AnsiConsole.MarkupLine($"[green]Current Manifest[/]");
				AnsiConsole.WriteLine(JsonConvert.SerializeObject(response.Item1, Formatting.Indented));
				AnsiConsole.MarkupLine($"[green]Current Status[/]");
				AnsiConsole.WriteLine(JsonConvert.SerializeObject(response.Item2, Formatting.Indented));
			}
		}
		else
		{
			await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
			_localBeamo.SaveBeamoLocalRuntime();

			if (!args.AsJson)
			{
				var serviceDefinitions = _localBeamo.BeamoManifest.ServiceDefinitions;
				var runningServiceInstances = _localBeamo.BeamoRuntime.ExistingLocalServiceInstances;

				var table = new Table();
				// Beam-O Id (Markup) | Image Id (markup) | Should Be Enabled on Deployed | Containers (borderless 2 column table with all containers -- markup | emoji (for status)) 
				var beamoIdColumn = new TableColumn(new Markup("Beam-O Id", columnNameStyle));
				var imageNameColumn = new TableColumn(new Markup("Image Id", columnNameStyle));
				var containersColumn = new TableColumn(new Markup("Local Running Containers", columnNameStyle));

				var shouldEnableOnRemoteDeployColumn = new TableColumn(new Markup("Should Enable On Remote Deploy", columnNameStyle));
				var canBeBuiltLocally = new TableColumn(new Markup("Can be Built Locally", columnNameStyle));

				table.AddColumn(beamoIdColumn).AddColumn(imageNameColumn).AddColumn(containersColumn).AddColumn(shouldEnableOnRemoteDeployColumn).AddColumn(canBeBuiltLocally);
				foreach (var sd in serviceDefinitions)
				{
					var beamoIdMarkup = new Markup($"[green]{sd.BeamoId}[/]");
					var imageIdMarkup = new Markup($"{sd.TruncImageId}");
					var shouldBeEnabledOnDeployMarkup = new Markup(sd.ShouldBeEnabledOnRemote ? "[green]Enable[/]" : "[red]Disable[/]");
					var isRemoteOnlyMarkup = new Markup(_localBeamo.VerifyCanBeBuiltLocally(sd) ? "[green]True[/]" : "[red]False[/]");

					IRenderable containersRenderable;
					var existingServiceInstances = runningServiceInstances.Where(si => si.BeamoId == sd.BeamoId).ToList();
					if (existingServiceInstances.Count == 0)
					{
						containersRenderable = new Markup("[yellow]-------------------[/]");
					}
					else
					{
						var containersTable = new Table();
						var containerNameColumn = new TableColumn("Container Name");
						var containerStatusColumn = new TableColumn("Container Status");
						var containerPortMappingsColumn = new TableColumn("Container Port Mappings");
						containersTable.AddColumn(containerNameColumn);
						containersTable.AddColumn(containerStatusColumn);
						containersTable.AddColumn(containerPortMappingsColumn);
						foreach (var existingServiceInstance in existingServiceInstances)
						{
							var containerNameMarkup = new Markup(existingServiceInstance.ContainerName);
							var containerStatusMarkup = new Markup(existingServiceInstance.IsRunning ? "[green]On[/]" : "[red]Off[/]");
							var containerPortMappingMarkup = new Markup(string.Join(", ", existingServiceInstance.ActivePortBindings.Select(p => $"{p.LocalPort}:{p.InContainerPort}")));
							containersTable.AddRow(containerNameMarkup, containerStatusMarkup, containerPortMappingMarkup);
						}

						containersRenderable = containersTable;
					}

					table.AddRow(new TableRow(new[] { beamoIdMarkup, imageIdMarkup, containersRenderable, shouldBeEnabledOnDeployMarkup, isRemoteOnlyMarkup, }));
				}

				AnsiConsole.Write(table);
			}
			else
			{
				AnsiConsole.WriteLine(JsonConvert.SerializeObject(_localBeamo.BeamoManifest, Formatting.Indented));
			}

			_localBeamo.SaveBeamoLocalManifest();
			_localBeamo.SaveBeamoLocalRuntime();
			await _localBeamo.StopListeningToDocker();
		}
	}
}
