using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using System.Net;

namespace cli;

public class ServicesListCommandArgs : LoginCommandArgs
{
	public bool Remote;
	public bool AsJson;
}

public class ServicesListCommand : AppCommand<ServicesListCommandArgs>, IResultSteam<DefaultStreamResultChannel, ServiceListResult>
{
	private IAppContext _ctx;
	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;

	public ServicesListCommand() :
		base("ps",
			"Lists the current local or remote service manifest and status (as summary table or json)")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--remote", "Makes it so that we output the current realm's remote manifest, instead of the local one"),
			(args, i) => args.Remote = i);

		AddOption(new Option<bool>("--json", "Outputs as json instead of summary table"),
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

		var isDockerRunning = await _localBeamo.CheckIsRunning();
		var serviceDefinitions = _localBeamo.BeamoManifest.ServiceDefinitions;
		var dependenciesDict = _localBeamo.GetAllBeamoIdsDependencies();
		var localServiceListResult = new ServiceListResult(!args.Remote, isDockerRunning, serviceDefinitions.Count);
		if (!isDockerRunning && !args.Remote)
		{

			var table = new Table();
			var beamoIdColumn = new TableColumn(new Markup("Beam-O Id", columnNameStyle));
			var imageNameColumn = new TableColumn(new Markup("Image Id", columnNameStyle));
			var shouldBeRunningColumn = new TableColumn(new Markup("Should be Running", columnNameStyle));
			var isRunningColumn = new TableColumn(new Markup("Is Running", columnNameStyle));
			table.AddColumn(beamoIdColumn).AddColumn(imageNameColumn).AddColumn(shouldBeRunningColumn).AddColumn(isRunningColumn);
			foreach (var sd in serviceDefinitions)
			{
				var beamoIdMarkup = new Markup($"[green]{sd.BeamoId}[/]");
				var imageIdMarkup = new Markup($"{sd.TruncImageId}");
				var shouldBeEnabledOnDeployMarkup = new Markup(sd.ShouldBeEnabledOnRemote ? "[green]Enable[/]" : "[red]Disable[/]");
				var isRemoteOnlyMarkup = new Markup(_localBeamo.VerifyCanBeBuiltLocally(sd) ? "[green]True[/]" : "[red]False[/]");
				localServiceListResult.AddService(sd.BeamoId, sd.ShouldBeEnabledOnRemote, false, sd.Protocol.ToString(), sd.ImageId,
					"", "", new[] { "" }, new[] { "" }, dependenciesDict[sd].Select(dep => dep.name));

				table.AddRow(new TableRow(new[] { beamoIdMarkup, imageIdMarkup, shouldBeEnabledOnDeployMarkup, isRemoteOnlyMarkup, }));
			}

			this.SendResults(localServiceListResult);

			var warning = new Panel("No docker running --- the running information here is not up-to-date!") { Header = new PanelHeader("NO DOCKER RUNNING") };
			AnsiConsole.Write(warning);
			AnsiConsole.Write(table);
			return;
		}

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

				foreach (var def in serviceDefinitions)
				{
					var beamoId = def.BeamoId;
					var imageId = def.ImageId;
					bool enableOnRemote = def.ShouldBeEnabledOnRemote;
					var isRunning = false;
					IEnumerable<string> dependencies = new List<string>();

					var service = manifest.manifest.FirstOrDefault(d => d.serviceName == beamoId);
					var storage = manifest.storageReference.FirstOrDefault(d => d.id == beamoId);

					if (service == null && storage == null)
					{
						continue;
					}

					if (def.Protocol == BeamoProtocolType.HttpMicroservice && service != null)
					{
						isRunning = status.services.First(s => s.serviceName == beamoId).running;
						dependencies = service.dependencies.Select(d => d.id);
						enableOnRemote = service.enabled;
					}
					else if (storage != null)
					{
						enableOnRemote = storage.enabled;
					}

					localServiceListResult.AddService(def.BeamoId,
						enableOnRemote,
						isRunning,
						def.Protocol.ToString(),
						imageId,
						"",
						"",
						new[] { "" },
						new[] { "" },
						dependencies);

					var beamoIdMarkup = new Markup(def.BeamoId);
					var imageIdMarkup = new Markup(imageId);
					var remoteTargetStatusMarkup = new Markup(enableOnRemote ? "[green]Should be Enabled[/]" : "[red]Should be Disabled[/]");
					var remoteStatusMarkup = new Markup(isRunning ? "[green]On[/]" : "[red]Off[/]");
					table.AddRow(new TableRow(new[] { beamoIdMarkup, imageIdMarkup, remoteTargetStatusMarkup, remoteStatusMarkup }));
				}

				this.SendResults(localServiceListResult);
				if (manifest.manifest.Count > 0)
				{
					AnsiConsole.Write(table);
				}
				else
				{
					AnsiConsole.WriteLine("No services found");
				}
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
					var hasNoRunningInstances = existingServiceInstances.Count == 0;
					if (hasNoRunningInstances)
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

							var portMappings = existingServiceInstance.ActivePortBindings.Select(p => $"{p.LocalPort}:{p.InContainerPort}");
							var containerPortMappingMarkup = new Markup(string.Join(", ", portMappings));
							containersTable.AddRow(containerNameMarkup, containerStatusMarkup, containerPortMappingMarkup);
						}

						containersRenderable = containersTable;
					}

					localServiceListResult.AddService(sd.BeamoId,
						sd.ShouldBeEnabledOnRemote,
						!hasNoRunningInstances,
						sd.Protocol.ToString(),
						sd.ImageId,
						"",
						"",
						new[] { "" },
						new[] { "" },
						dependenciesDict[sd].Select(dep => dep.name));
					table.AddRow(new TableRow(new[] { beamoIdMarkup, imageIdMarkup, containersRenderable, shouldBeEnabledOnDeployMarkup, isRemoteOnlyMarkup, }));
				}

				this.SendResults(localServiceListResult);
				if (serviceDefinitions.Count > 0)
				{
					AnsiConsole.Write(table);
				}
				else
				{
					AnsiConsole.WriteLine("No services found");
				}
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

public class ServiceListResult
{
	public bool IsLocal;
	public bool IsDockerRunning;

	public List<string> BeamoIds;
	public List<bool> ShouldBeEnabledOnRemote;
	public List<bool> RunningState;

	public List<string> ProtocolTypes;
	public List<string> ImageIds;

	public List<string> ContainerNames;
	public List<string> ContainerIds;
	public List<string> LocalHostPorts;
	public List<string> LocalContainerPorts;

	public List<string> Dependencies;

	public ServiceListResult(bool local, bool isDockerRunning, int allocateCount)
	{
		IsLocal = local;
		IsDockerRunning = isDockerRunning;

		BeamoIds = new List<string>(allocateCount);

		ShouldBeEnabledOnRemote = new List<bool>(allocateCount);
		RunningState = new List<bool>(allocateCount);

		ProtocolTypes = new List<string>(allocateCount);
		ImageIds = new List<string>(allocateCount);

		ContainerNames = new List<string>(allocateCount);
		ContainerIds = new List<string>(allocateCount);
		LocalHostPorts = new List<string>(allocateCount);
		LocalContainerPorts = new List<string>(allocateCount);

		Dependencies = new List<string>(allocateCount);
	}

	public void AddService(string beamoId, bool shouldBeEnabledOnRemote, bool running, string protocol, string imageId,
		string containerName, string containerId, IEnumerable<string> hostPort, IEnumerable<string> containerPort, IEnumerable<string> dependentBeamoIds)
	{
		BeamoIds.Add(beamoId);
		RunningState.Add(running);
		ShouldBeEnabledOnRemote.Add(shouldBeEnabledOnRemote);

		ProtocolTypes.Add(protocol);
		ImageIds.Add(imageId);

		ContainerNames.Add(containerName);
		ContainerIds.Add(containerId);
		LocalHostPorts.Add(string.Join(",", hostPort.ToList()));
		LocalContainerPorts.Add(string.Join(",", containerPort.ToList()));

		var dependencies = string.Join(",", dependentBeamoIds.ToList());

		Dependencies.Add(dependencies);
	}
}
