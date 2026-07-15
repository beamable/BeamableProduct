using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using cli.Services;

namespace cli.Commands.Project;

public class ListCommandArgs : CommandArgs
{

}

[Serializable]
public class ListCommandResult
{
	public List<ServiceInfo> localServices;
	public List<ServiceInfo> localStorages;
	public List<ServiceInfo> localPortalExtensions;
}

public class ListCommand : AtomicCommand<ListCommandArgs, ListCommandResult>
{
	public ListCommand() : base("list",
		"Get a list of microservices, storages, and portal extensions known to the local manifest")
	{
	}

	public override void Configure()
	{
	}

	public override Task<ListCommandResult> GetResult(ListCommandArgs args)
	{
		// `ProjectDirectory` on a ServiceDefinition is relative to the CLI's
		// execution dir, which is rarely useful to consumers. Convert to a
		// path that's relative to the .beamable workspace root so callers
		// (e.g. the sandbox file API, which enforces workspace containment)
		// can use it directly without further math.
		string ToWorkspaceRelative(string p) =>
			string.IsNullOrEmpty(p) ? p : args.ConfigService.GetRelativeToBeamableWorkspacePath(p);

		var services = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(definition => definition.Protocol == BeamoProtocolType.HttpMicroservice).Select(
				definition => new ServiceInfo() { name = definition.BeamoId, projectPath = ToWorkspaceRelative(definition.ProjectDirectory) });
		var storages = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(definition => definition.Protocol == BeamoProtocolType.EmbeddedMongoDb).Select(
				definition => new ServiceInfo() { name = definition.BeamoId, projectPath = ToWorkspaceRelative(definition.ProjectDirectory) });
		var portalExtensions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(definition => definition.Protocol == BeamoProtocolType.PortalExtension).Select(
				definition => new ServiceInfo() { name = definition.BeamoId, projectPath = ToWorkspaceRelative(definition.ProjectDirectory) });

		Log.Debug("Found {} services", args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Count);

		return Task.FromResult(new ListCommandResult
		{
			localServices = services.ToList(),
			localStorages = storages.ToList(),
			localPortalExtensions = portalExtensions.ToList(),
		});
	}
}
