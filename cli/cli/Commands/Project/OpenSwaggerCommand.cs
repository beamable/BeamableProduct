using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using System.CommandLine;

namespace cli.Dotnet;

public class OpenSwaggerCommandArgs : CommandArgs
{
	public bool isRemote;
	public ServiceName serviceName;
}

public class OpenSwaggerCommand : AppCommand<OpenSwaggerCommandArgs>, IEmptyResult
{
	public OpenSwaggerCommand() : base("open-swagger", "Opens the swagger page for a given service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("service-name",()=> new ServiceName(), "Name of the service to open swagger to"), (arg, i) => arg.serviceName = i);
		AddOption(new Option<bool>("--remote", "If passed, swagger will open to the remote version of this service. Otherwise, it will try and use the local version"), (arg, i) => arg.isRemote = i);
	}

	public override Task Handle(OpenSwaggerCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.serviceName))
		{
			var serviceDefinitions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(definition => definition.Protocol == BeamoProtocolType.HttpMicroservice).ToList();
			if (serviceDefinitions.Count == 1)
			{
				args.serviceName = new ServiceName(serviceDefinitions[0].BeamoId);
				BeamableLogger.Log(
					$"No service-name passed as argument. Running command for {args.serviceName} since it is the only one microservice in BeamoManifest.");
			}
			else
			{
				throw new CliException("No service-name passed as argument.");
			}
		}
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var url = $"{args.AppContext.Host.Replace("dev.", "dev-").Replace("api", "portal")}/{cid}/games/{pid}/realms/{pid}/microservices/{args.serviceName}/docs";
		if (!args.isRemote)
		{
			url += $"?prefix={MachineHelper.GetUniqueDeviceId()}";
		}
		MachineHelper.OpenBrowser(url);
		return Task.CompletedTask;
	}
}
