using Beamable.Common.BeamCli.Contracts;
using Serilog;

namespace cli.Commands.Project;

public class ListCommandArgs : CommandArgs
{

}

[Serializable]
public class ListCommandResult
{
	public List<ServiceInfo> localServices;
}

public class ListCommand : AppCommand<ListCommandArgs>, IResultSteam<DefaultStreamResultChannel, ListCommandResult>
{
	public ListCommand() : base("list",
		"Get a list of microservices")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(ListCommandArgs args)
	{
		Log.Information("Running list command " + args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Count);

		var services = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.Select(x => new ServiceInfo
		{
			name = x.Key,
			dockerfilePath = x.Value.RelativeDockerfilePath,
			dockerBuildPath = x.Value.DockerBuildContextPath
		}).ToList();

		this.SendResults(new ListCommandResult
		{
			localServices = services
		});
		Log.Information("Sent list command " + args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Count);

		return Task.CompletedTask;
	}
}
