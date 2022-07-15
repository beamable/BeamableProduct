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
	private readonly IAppContext _ctx;
	private readonly BeamoLocalService _localBeamo;
	private BeamoService _remoteBeamo;

	public ServicesListCommand(IAppContext ctx, BeamoLocalService localBeamo, BeamoService remoteRemoteBeamo) :
		base("ps",
			"Lists the current local or remote service manifest and status (as summary table or json).")
	{
		_ctx = ctx;
		_localBeamo = localBeamo;
		_remoteBeamo = remoteRemoteBeamo;
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
		var titleText = !args.Remote ? "Local Services Status" : "Remote Services Status";
		AnsiConsole.MarkupLine($"[lightskyblue1]{titleText}[/]");

		// Declare style for column headers
		var columnNameStyle = new Style(Color.SlateBlue1);

		if (args.Remote)
		{
			var response = await AnsiConsole.Status()
				.Spinner(Spinner.Known.Default)
				.StartAsync("Sending Request...", async ctx =>
					await _remoteBeamo.GetStatus()
				);

			if (!args.AsJson)
			{
				var table = new Table();
				var beamoIdColumn = new TableColumn(new Markup("Beam-O Id", columnNameStyle));
				var imageNameColumn = new TableColumn(new Markup("Image Id", columnNameStyle));
				var tryEnableOnRemoteDeployColumn = new TableColumn(new Markup("Is Running", columnNameStyle));
				table.AddColumn(beamoIdColumn).AddColumn(imageNameColumn).AddColumn(tryEnableOnRemoteDeployColumn);

				foreach (var responseService in response.services)
				{
					var beamoId = new Markup(responseService.serviceName);
					var imageId = new Markup(responseService.imageId.Split('/')[1]);
					var remoteStatus = new Markup(responseService.running ? "[green]On[/]" : "[red]Off[/]");
					table.AddRow(new TableRow(new[] { beamoId, imageId, remoteStatus }));
				}

				AnsiConsole.Write(table);
			}
			else
			{
				AnsiConsole.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
			}
		}
		else
		{
			await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest.ServiceDefinitions, _localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
			_localBeamo.SaveBeamoLocalRuntime();

			if (!args.AsJson)
			{
				var serviceDefinitions = _localBeamo.BeamoManifest.ServiceDefinitions;
				var runningServiceInstances = _localBeamo.BeamoRuntime.ExistingLocalServiceInstances;
				var beamoIdsToTryEnableOnRemoteDeploy = _localBeamo.BeamoRuntime.BeamoIdsToTryEnableOnRemoteDeploy;

				var table = new Table();
				// Beam-O Id (Markup) | Image Id (markup) | Should Be Enabled on Deployed | Containers (borderless 2 column table with all containers -- markup | emoji (for status)) 
				var beamoIdColumn = new TableColumn(new Markup("Beam-O Id", columnNameStyle));
				var imageNameColumn = new TableColumn(new Markup("Image Id", columnNameStyle));
				var tryEnableOnRemoteDeployColumn = new TableColumn(new Markup("Try Enable On Remote Deploy", columnNameStyle));
				var containersColumn = new TableColumn(new Markup("Existing Containers", columnNameStyle));
				table.AddColumn(beamoIdColumn).AddColumn(imageNameColumn).AddColumn(tryEnableOnRemoteDeployColumn).AddColumn(containersColumn);
				foreach (var serviceDefinition in serviceDefinitions)
				{
					var beamoId = serviceDefinition.BeamoId;
					var imageId = serviceDefinition.ImageId;
					var tryEnableOnDeploy = beamoIdsToTryEnableOnRemoteDeploy.Contains(beamoId);
					var existingServiceInstances = runningServiceInstances.Where(si => si.BeamoId == beamoId).ToList();

					var beamoIdMarkup = new Markup($"[green]{beamoId}[/]");
					var imageIdMarkup = new Markup($"{imageId}");
					var shouldBeEnabledOnDeployMarkup = new Markup(tryEnableOnDeploy ? "[green]Enabled[/]" : "[red]Disabled[/]");

					IRenderable containersRenderable;
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

					table.AddRow(new TableRow(new[] { beamoIdMarkup, imageIdMarkup, shouldBeEnabledOnDeployMarkup, containersRenderable }));
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
