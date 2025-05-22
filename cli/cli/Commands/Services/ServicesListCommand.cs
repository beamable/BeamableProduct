using cli.Services;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using Beamable.Server;

namespace cli;

public class ServicesListCommandArgs : LoginCommandArgs
{
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
		AddOption(new Option<bool>("--json", "Outputs as json instead of summary table"),
			(args, i) => args.AsJson = i);
	}

	public override async Task Handle(ServicesListCommandArgs args)
	{
		_ctx = args.AppContext;
		_localBeamo = args.BeamoLocalSystem;
		_remoteBeamo = args.BeamoService;

		var titleText = "Services Status";
		AnsiConsole.MarkupLine($"[lightskyblue1]{titleText}[/]");


		// Declare style for column headers
		var columnNameStyle = new Style(Color.SlateBlue1);

		var isDockerRunning = await _localBeamo.CheckIsRunning();
		var serviceDefinitions = _localBeamo.BeamoManifest.ServiceDefinitions;
		var dependenciesDict = _localBeamo.GetAllBeamoIdsDependencies(getAll: true);
		var localServiceListResult = new ServiceListResult(isDockerRunning, serviceDefinitions.Count);
		if (isDockerRunning)
		{
			await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
			_localBeamo.SaveBeamoLocalRuntime();
		}

		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetStatus()
			);

		Log.Verbose($"Got status=[{JsonConvert.SerializeObject(response)}]");

		if (!args.AsJson)
		{
			var runningServiceInstances = _localBeamo.BeamoRuntime.ExistingLocalServiceInstances;

			var table = new Table();
			// Beam-O Id (Markup) | Image Id (markup) | Should Be Enabled on Deployed | Containers (borderless 2 column table with all containers -- markup | emoji (for status))
			var beamoIdColumn = new TableColumn(new Markup("Beam-O Id", columnNameStyle));
			var imageNameColumn = new TableColumn(new Markup("Image Id", columnNameStyle));
			var containersColumn = new TableColumn(new Markup("Local Running Containers", columnNameStyle));
			var runningRemoteColumn = new TableColumn(new Markup("Is Running Remotely", columnNameStyle));

			var shouldEnableOnRemoteDeployColumn = new TableColumn(new Markup("Should Enable On Remote Deploy", columnNameStyle));
			var canBeBuiltLocally = new TableColumn(new Markup("Can be Built Locally", columnNameStyle));

			table.AddColumn(beamoIdColumn).AddColumn(imageNameColumn).AddColumn(containersColumn).AddColumn(shouldEnableOnRemoteDeployColumn).AddColumn(canBeBuiltLocally).AddColumn(runningRemoteColumn);
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

				var remoteService = response.services.FirstOrDefault(s => s.serviceName == sd.BeamoId);
				var isRunningOnRemote = remoteService != null && response.services.First(s => s.serviceName == sd.BeamoId).running;
				var isRunningRemotelyMark = new Markup(isRunningOnRemote ? "[green]True[/]" : "[red]False[/]");

				localServiceListResult.AddService(sd.BeamoId,
					sd.ShouldBeEnabledOnRemote,
					!hasNoRunningInstances,
					sd.Protocol.ToString(),
					sd.ImageId,
					"",
					"",
					new[] { "" },
					new[] { "" },
					dependenciesDict[sd].Where(d => d.type.Equals("storage")).Select(dep => dep.name),
					dependenciesDict[sd].Where(d => d.type.Equals("unity-asmdef")).Select(dep => dep.dllName),
					sd.ProjectDirectory,
					sd.IsLocal,
					sd.IsInRemote,
					isRunningOnRemote);
				table.AddRow(new TableRow(new[] { beamoIdMarkup, imageIdMarkup, containersRenderable, shouldBeEnabledOnDeployMarkup, isRemoteOnlyMarkup, isRunningRemotelyMark}));
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
			AnsiConsole.MarkupLine($"[green]Current Manifest[/]");
			AnsiConsole.WriteLine(JsonConvert.SerializeObject(_localBeamo.BeamoManifest, Formatting.Indented));
			AnsiConsole.MarkupLine($"[green]Current Status[/]");
			AnsiConsole.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
		}

		_localBeamo.SaveBeamoLocalRuntime();
		if (!isDockerRunning)
		{
			var warning = new Panel("No docker running --- the running information here is not up-to-date!") { Header = new PanelHeader("NO DOCKER RUNNING") };
			AnsiConsole.Write(warning);
			await _localBeamo.StopListeningToDocker();
		}
	}
}

public class ServiceListResult
{
	/// <summary>
	/// True if the service exists in a local directory
	/// </summary>
	public List<bool> ExistInLocal;

	/// <summary>
	/// True if the service was published before and exists in the cloud
	/// </summary>
	public List<bool> ExistInRemote;
	
	public List<bool> IsRunningRemotely;
	public bool IsDockerRunning;

	public List<string> BeamoIds;
	public List<bool> ShouldBeEnabledOnRemote;
	public List<bool> IsRunningLocally;

	public List<string> ProtocolTypes;
	public List<string> ImageIds;

	public List<string> ContainerNames;
	public List<string> ContainerIds;
	public List<string> LocalHostPorts;
	public List<string> LocalContainerPorts;

	public List<string> Dependencies;
	public List<string> ProjectPath;
	public List<string> UnityAssemblyDefinitions;

	public ServiceListResult( bool isDockerRunning, int allocateCount)
	{
		ExistInLocal = new List<bool>(allocateCount);;
		ExistInRemote = new List<bool>(allocateCount);;
		IsRunningRemotely = new List<bool>(allocateCount);;
		IsDockerRunning = isDockerRunning;

		BeamoIds = new List<string>(allocateCount);

		ShouldBeEnabledOnRemote = new List<bool>(allocateCount);
		IsRunningLocally = new List<bool>(allocateCount);

		ProtocolTypes = new List<string>(allocateCount);
		ImageIds = new List<string>(allocateCount);

		ContainerNames = new List<string>(allocateCount);
		ContainerIds = new List<string>(allocateCount);
		LocalHostPorts = new List<string>(allocateCount);
		LocalContainerPorts = new List<string>(allocateCount);

		Dependencies = new List<string>(allocateCount);
		ProjectPath = new List<string>(allocateCount);
		UnityAssemblyDefinitions = new List<string>(allocateCount);
	}

	public void AddService(string beamoId, bool shouldBeEnabledOnRemote, bool runningLocally, string protocol, string imageId,
		string containerName, string containerId, IEnumerable<string> hostPort, IEnumerable<string> containerPort, IEnumerable<string> dependentBeamoIds, IEnumerable<string> unityAssemblyNames,
		string projectPath, bool isLocal, bool isInRemote, bool isRunningRemotely)
	{
		BeamoIds.Add(beamoId);
		IsRunningLocally.Add(runningLocally);
		ShouldBeEnabledOnRemote.Add(shouldBeEnabledOnRemote);

		ProtocolTypes.Add(protocol);
		ImageIds.Add(imageId);

		ContainerNames.Add(containerName);
		ContainerIds.Add(containerId);
		LocalHostPorts.Add(string.Join(",", hostPort.ToList()));
		LocalContainerPorts.Add(string.Join(",", containerPort.ToList()));

		var dependencies = string.Join(",", dependentBeamoIds.ToList());

		Dependencies.Add(dependencies);
		UnityAssemblyDefinitions.Add(string.Join(",", unityAssemblyNames));
		ProjectPath.Add(projectPath);
		ExistInLocal.Add(isLocal);
		ExistInRemote.Add(isInRemote);
		IsRunningRemotely.Add(isRunningRemotely);
	}
}
