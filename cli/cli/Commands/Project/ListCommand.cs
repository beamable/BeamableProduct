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
}

public class ListCommand : AtomicCommand<ListCommandArgs, ListCommandResult>
{
	public ListCommand() : base("list",
		"Get a list of microservices")
	{
	}

	public override void Configure()
	{
	}

	public override Task<ListCommandResult> GetResult(ListCommandArgs args)
	{
		var services = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(definition => definition.Protocol == BeamoProtocolType.HttpMicroservice).Select(
				definition => new ServiceInfo() { name = definition.BeamoId, projectPath = definition.ProjectDirectory });
		var storages = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(definition => definition.Protocol == BeamoProtocolType.EmbeddedMongoDb).Select(
				definition => new ServiceInfo() { name = definition.BeamoId, projectPath = definition.ProjectDirectory });

		Log.Debug("Found {} services", args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Count);

		return Task.FromResult(new ListCommandResult { localServices = services.ToList(), localStorages = storages.ToList() });
	}
}
