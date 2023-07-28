using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using Spectre.Console;
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
		AddArgument(new Argument<ServiceName>("service-name", () => new ServiceName(), "Name of the service to open swagger to"), (arg, i) => arg.serviceName = i);
		AddOption(new Option<bool>("--remote", "If passed, swagger will open to the remote version of this service. Otherwise, it will try and use the local version"), (arg, i) => arg.isRemote = i);
	}

	public override Task Handle(OpenSwaggerCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.serviceName))
		{
			var serviceDefinitions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(definition => definition.Protocol == BeamoProtocolType.HttpMicroservice).ToList();

			switch (serviceDefinitions.Count)
			{
				case 1:
					args.serviceName = new ServiceName(serviceDefinitions[0].BeamoId);
					BeamableLogger.Log(
						$"No service-name passed as argument. Running command for {args.serviceName} since it is the only microservice in BeamoManifest.");
					break;
				case > 1:
					BeamableLogger.Log("We found more than one microservices in the directory");
					AskForServiceNameAndExecuteBeamCommandTask(serviceDefinitions, args,
						!string.IsNullOrWhiteSpace(args.AppContext.WorkingDirectory)
							? args.AppContext.WorkingDirectory
							: "");
					return Task.CompletedTask;
				default:
					BeamableLogger.Log("We couldn't find a service name in the directory");
					AskForDirectoryAndExecuteBeamCommandTask(args);
					return Task.CompletedTask;
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

	private static void AskForDirectoryAndExecuteBeamCommandTask(OpenSwaggerCommandArgs args)
	{
		string directory = AnsiConsole.Ask<string>("Enter the absolute or relative directory to use:");
		new BeamCommandAssistantBuilder("project open-swagger")
			.WithOption(true, "--dir", directory)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.Host), "--host", args.AppContext.Host)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.Cid), "--cid", args.AppContext.Cid)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.Pid), "--pid", args.AppContext.Pid)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.RefreshToken), "--refresh-token", args.AppContext.RefreshToken)
			.WithOption(args.AppContext.IsDryRun, "--dryrun", "")
			.WithOption(args.isRemote, "--remote", "")
			.ExecuteAsync();
	}

	private static void AskForServiceNameAndExecuteBeamCommandTask(
		IEnumerable<BeamoServiceDefinition> serviceDefinitions, OpenSwaggerCommandArgs args, string directory)
	{
		string serviceName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select the service name to use:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more service name)[/]")
				.AddChoices(serviceDefinitions.Select(serviceDef => serviceDef.BeamoId)));

		new BeamCommandAssistantBuilder("project open-swagger")
			.AddArgument(serviceName)
			.WithOption(!string.IsNullOrWhiteSpace(directory), "--dir", directory)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.Host), "--host", args.AppContext.Host)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.Cid), "--cid", args.AppContext.Cid)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.Pid), "--pid", args.AppContext.Pid)
			.WithOption(!string.IsNullOrWhiteSpace(args.AppContext.RefreshToken), "--refresh-token", args.AppContext.RefreshToken)
			.WithOption(args.AppContext.IsDryRun, "--dryrun", "")
			.WithOption(args.isRemote, "--remote", "")
			.ExecuteAsync();
	}
}
