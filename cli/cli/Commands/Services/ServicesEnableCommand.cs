using Beamable.Common;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesEnableCommandArgs : LoginCommandArgs
{
	public string BeamoId;
	public bool EnableOnRemoteDeploy;
	public bool? IgnoreDependencies;
}

public class ServicesEnableCommand : AppCommand<ServicesEnableCommandArgs>
{
	public static readonly Option<string> BEAM_SERVICE_OPTION_ID = new("--id", "The Unique Id for this service within this Beamable CLI context");
	public static readonly Option<bool> BEAM_SERVICE_OPTION_ENABLE_ON_REMOTE_DEPLOY = new("--enabled", "Whether or not we should try and run the service when we deploy remotely");
	public static readonly Option<bool?> BEAM_SERVICE_OPTION_IGNORE_DEPENDENCIES = new("--no-deps", () => null, "Propagates the change to the services dependencies. When disabling, this is true by default");

	private BeamoLocalSystem _localBeamo;


	public ServicesEnableCommand() :
		base("enable",
			"Enables/Disables existing services")
	{
	}

	public override void Configure()
	{
		AddOption(BEAM_SERVICE_OPTION_ID, (args, i) => args.BeamoId = i);
		AddOption(BEAM_SERVICE_OPTION_ENABLE_ON_REMOTE_DEPLOY, (args, i) => args.EnableOnRemoteDeploy = i);
		AddOption(BEAM_SERVICE_OPTION_IGNORE_DEPENDENCIES, (args, i) => args.IgnoreDependencies = i);
	}

	private static bool EnsureEnableOnRemoteDeploy(ref bool enabled, bool current = false)
	{
		enabled = AnsiConsole.Prompt(new SelectionPrompt<bool>()
			.Title($"Do you wish for us to try and run this service when you deploy it remotely? [lightskyblue1](Current: {current})[/]")
			.AddChoices(new[] { true, false })
			.AddBeamHightlight());

		return true;
	}

	public override async Task Handle(ServicesEnableCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;
		var hasAnyServices = _localBeamo.BeamoManifest.ServiceDefinitions.Count > 0;
		if (!hasAnyServices)
		{
			BeamableLogger.LogError("There are no services in BeamoManifest.");
		}

		// Handle Microservice Beamo Id Option 
		var existingMicroserviceBeamoIds = _localBeamo.BeamoManifest.ServiceDefinitions
			.Where(c => c.Protocol == BeamoProtocolType.HttpMicroservice)
			.Select(c => c.BeamoId)
			.ToList();

		if (string.IsNullOrEmpty(args.BeamoId))
		{
			args.BeamoId = AnsiConsole.Prompt(new SelectionPrompt<string>()
				.Title("Choose the [lightskyblue1]Beamo-O Service[/] to Modify:")
				.AddChoices(existingMicroserviceBeamoIds)
				.AddBeamHightlight());
		}

		var serviceDefinition = _localBeamo.BeamoManifest.ServiceDefinitions.FirstOrDefault(sd => sd.BeamoId == args.BeamoId);

		if (serviceDefinition == null)
		{
			var root = new Tree($"Could not find [bold][red]{args.BeamoId}[/][/] service. These are available services:");
			foreach (var beamoServiceDefinition in _localBeamo.BeamoManifest.ServiceDefinitions)
			{
				root.AddNode($"[[{beamoServiceDefinition.Protocol.ToString()}]] [bold]{beamoServiceDefinition.BeamoId}[/]");
			}
			AnsiConsole.Write(root);
			return;
		}

		// For now, we only support enabling/disabling the HttpMicroservice protocol services --- TODO this should change in the future...
		if (serviceDefinition.Protocol != BeamoProtocolType.HttpMicroservice)
		{
			AnsiConsole.WriteException(new ArgumentOutOfRangeException(nameof(args.BeamoId), "You cannot enable/disable non-HttpMicroservice Services directly." +
																							 " Please make this service a dependency of a HttpMicroservice and then enable/disable that service instead."));
			return;
		}

		// Even though this is not something exclusive to Microservices, we only use it for HttpMicroservices at the moment.
		_ = EnsureEnableOnRemoteDeploy(ref args.EnableOnRemoteDeploy, serviceDefinition.ShouldBeEnabledOnRemote);

		// If we are disabling, by default we should ignore dependencies when disabling and enforce them when enabling
		args.IgnoreDependencies ??= !args.EnableOnRemoteDeploy;

		// Update the service definition
		serviceDefinition.ShouldBeEnabledOnRemote = args.EnableOnRemoteDeploy;

		// If we are supposed to propagate the changes to all service dependencies, let's do that.
		if (!args.IgnoreDependencies.Value)
		{
			var dependencies = await args.BeamoLocalSystem.GetDependencies(serviceDefinition.BeamoId);
			foreach (var id in dependencies)
			{
				var dep = _localBeamo.BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == id);
				dep.ShouldBeEnabledOnRemote = args.EnableOnRemoteDeploy;
			}
		}

		_localBeamo.SaveBeamoLocalManifest();
		_localBeamo.SaveBeamoLocalRuntime();

		await _localBeamo.StopListeningToDocker();
	}
}
