using cli.Services;
using Serilog;
using System.CommandLine;

namespace cli;

public class ServicesUpdateDockerfileCommandArgs : CommandArgs
{
	public string ServiceName;
}

public class ServicesUpdateDockerfileCommand : AppCommand<ServicesUpdateDockerfileCommandArgs>, IEmptyResult
{
	public ServicesUpdateDockerfileCommand() : base("update-dockerfile", "Updates the Dockerfile for the specified service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("ServiceName", description: "The name of the microservice to udpate the Dockerfile"),
			(args, i) => args.ServiceName = i);
	}

	public override async Task Handle(ServicesUpdateDockerfileCommandArgs args)
	{
		if (!args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(args.ServiceName, out var serviceDefinition))
		{
			Log.Error($"The service {args.ServiceName} does not have a definition in the manifest");
			return;
		}

		if (serviceDefinition.Protocol != BeamoProtocolType.HttpMicroservice)
		{
			Log.Error($"The service {args.ServiceName} is not a HttpMicroservice");
			return;
		}

		await args.BeamoLocalSystem.UpdateDockerFile(serviceDefinition);
		Log.Information($"The service {args.ServiceName} Dockerfile has ben updated!");
	}
}
