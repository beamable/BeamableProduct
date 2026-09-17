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
		"Get a list of microservices, storages, and portal extensions")
	{
	}

	public override void Configure()
	{
	}

	public override Task<ListCommandResult> GetResult(ListCommandArgs args)
	{
		var definitions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions;

		List<ServiceInfo> Select(BeamoProtocolType protocol) => definitions
			.Where(definition => definition.Protocol == protocol)
			.Select(definition => new ServiceInfo() { name = definition.BeamoId, projectPath = definition.ProjectDirectory })
			.ToList();

		var services = Select(BeamoProtocolType.HttpMicroservice);
		var storages = Select(BeamoProtocolType.EmbeddedMongoDb);
		var portalExtensions = Select(BeamoProtocolType.PortalExtension);

		Log.Debug("Found {} service definitions", definitions.Count);

		return Task.FromResult(new ListCommandResult
		{
			localServices = services,
			localStorages = storages,
			localPortalExtensions = portalExtensions,
		});
	}
}
