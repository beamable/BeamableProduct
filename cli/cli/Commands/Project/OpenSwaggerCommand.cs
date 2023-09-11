using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class OpenSwaggerCommandArgs : CommandArgs
{
	public bool IsRemote;
	public ServiceName ServiceName;
}

public class OpenSwaggerCommand : AppCommand<OpenSwaggerCommandArgs>, IEmptyResult
{
	public OpenSwaggerCommand() : base("open-swagger", "Opens the swagger page for a given service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("service-name", () => new ServiceName(), "Name of the service to open swagger to"), (arg, i) => arg.ServiceName = i);
		AddOption(new Option<bool>("--remote", "If passed, swagger will open to the remote version of this service. Otherwise, it will try and use the local version"), (arg, i) => arg.IsRemote = i);
	}

	public override Task Handle(OpenSwaggerCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.ServiceName))
		{
			var serviceDefinitions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(definition => definition.Protocol == BeamoProtocolType.HttpMicroservice).ToList();

			switch (serviceDefinitions.Count)
			{
				case 1:
					args.ServiceName = new ServiceName(serviceDefinitions[0].BeamoId);
					BeamableLogger.Log(
						$"No service-name passed as argument. Running command for {args.ServiceName} since it is the only microservice in BeamoManifest.");
					break;
				case > 1:
					BeamableLogger.Log("We found more than one microservices in the directory");
					AskForServiceNameAndRunBeamCommandTask(serviceDefinitions, args,
						!string.IsNullOrWhiteSpace(args.AppContext.WorkingDirectory)
							? args.AppContext.WorkingDirectory
							: string.Empty);
					return Task.CompletedTask;
				default:
					BeamableLogger.Log("We couldn't find a service name in the directory");
					AskForDirectoryAndRunBeamCommandTask(args);
					return Task.CompletedTask;
			}
		}

		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var refreshToken = args.AppContext.RefreshToken;
		var queryArgs = new List<string>
		{
			$"refresh_token={refreshToken}",
			$"prefix={MachineHelper.GetUniqueDeviceId()}"
		};
		var joinedQueryString = string.Join("&", queryArgs);
		var url = $"{args.AppContext.Host.Replace("dev.", "dev-").Replace("api", "portal")}/{cid}/games/{pid}/realms/{pid}/microservices/{args.ServiceName}/docs?{joinedQueryString}";

		MachineHelper.OpenBrowser(url);
		return Task.CompletedTask;
	}

	private static async void AskForDirectoryAndRunBeamCommandTask(OpenSwaggerCommandArgs args)
	{
		string directory = AnsiConsole.Ask<string>("Enter the absolute or relative directory to use:");
		await new BeamCommandAssistantBuilder("project open-swagger", args.AppContext)
			.WithOption(true, "--dir", directory)
			.WithOption(args.IsRemote, "--remote")
			.RunAsync();
	}

	private static async void AskForServiceNameAndRunBeamCommandTask(
		IEnumerable<BeamoServiceDefinition> serviceDefinitions, OpenSwaggerCommandArgs args, string directory)
	{
		string serviceName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select the service name to use:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more service name)[/]")
				.AddChoices(serviceDefinitions.Select(serviceDef => serviceDef.BeamoId))
				.AddBeamHightlight());

		await new BeamCommandAssistantBuilder("project open-swagger", args.AppContext)
			.AddArgument(serviceName)
			.WithOption(!string.IsNullOrWhiteSpace(directory), "--dir", directory)
			.WithOption(args.IsRemote, "--remote")
			.RunAsync();
	}
}
