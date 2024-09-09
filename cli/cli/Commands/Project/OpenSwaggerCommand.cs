using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Semantics;
using cli.FederationCommands;
using cli.Services;
using cli.Utils;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class OpenSwaggerCommandArgs : CommandArgs
{
	public string RoutingKey;
	public ServiceName ServiceName;
}

public class OpenSwaggerCommand : AppCommand<OpenSwaggerCommandArgs>, IEmptyResult
{
	public OpenSwaggerCommand() : base("open-swagger", "Opens the swagger page for a given service")
	{
		AddAlias("swagger");
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("service-name", () => new ServiceName(), "Name of the service to open swagger to"), (arg, i) => arg.ServiceName = i);

		var defaultRoutingKey = ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine();
		
		var remoteOption = new Option<bool>("--remote",
			"When set, enforces the routing key to be the one for the service deployed to the realm. Cannot be specified when --routing-key is also set.");
		remoteOption.AddAlias("-r");
		
		var routingKeyOption = new Option<string>("--routing-key",
			"The routing key for the service instance we want. If not passed, defaults to the local service. ");
		routingKeyOption.SetDefaultValue(defaultRoutingKey);
		routingKeyOption.AddAlias("-k");
		
		AddOption(routingKeyOption, (arg, ctx, key) =>
		{
			var remoteValue = ctx.ParseResult.GetValueForOption(remoteOption);
			if (remoteValue)
			{
				if (key != defaultRoutingKey)
				{
					throw new CliException("Cannot specify both --routing-key and --remote");
				}
				else
				{
					key = "";
				}
			}

			arg.RoutingKey = key;
		});
		
		AddOption(remoteOption, (arg, i) =>
		{
			// nothing happens here, all logic handled in routingKeyOption binder
		});

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
					Log.Debug($"No service-name passed as argument. " +
					          $"Running command for {args.ServiceName} since it is the only microservice in BeamoManifest.");
					break;
				case > 1:
					Log.Information("Found more than one microservices in the directory");
					args.ServiceName = new ServiceName(AskForServiceNameAndRunBeamCommandTask(serviceDefinitions));
					break;
			}
		}

		if (string.IsNullOrWhiteSpace(args.ServiceName))
		{
			throw new CliException("No service found. Navigate to a .beamable workspace with valid Microservices. ");
		}

		OpenSwagger(args.AppContext, args.ServiceName, args.RoutingKey);
		return Task.CompletedTask;
	}

	public static void OpenSwagger(IAppContext ctx, string beamoId, string routingKey)
	{
		var cid = ctx.Cid;
		var pid = ctx.Pid;
		var refreshToken = ctx.RefreshToken;
		var queryArgs = new List<string>
		{
			$"refresh_token={refreshToken}",
		};
		
		if(!string.IsNullOrEmpty(routingKey))
			queryArgs.Add($"routingKey={routingKey}");
		
		var joinedQueryString = string.Join("&", queryArgs);
		var url = $"{ctx.Host.Replace("dev.", "dev-").Replace("api", "portal")}/{cid}/games/{pid}/realms/{pid}/microservices/micro_{beamoId}/docs?{joinedQueryString}";

		MachineHelper.OpenBrowser(url);
	}

	private static string AskForServiceNameAndRunBeamCommandTask(
		IEnumerable<BeamoServiceDefinition> serviceDefinitions)
	{
		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select the service name to use:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more service name)[/]")
				.AddChoices(serviceDefinitions.Select(serviceDef => serviceDef.BeamoId))
				.AddBeamHightlight());
	}
}
