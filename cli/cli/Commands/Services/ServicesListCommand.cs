using cli.Services;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using Beamable.Server;
using cli.Utils;

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
			ServicesDeletionNotice.REMOVED_PREFIX + "Lists the current local or remote service manifest and status (as summary table or json)")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--json", "Outputs as json instead of summary table"),
			(args, i) => args.AsJson = i);
	}

	public override async Task Handle(ServicesListCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.LIST_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
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
