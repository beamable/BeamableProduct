using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesRegisterCommandArgs : LoginCommandArgs
{
	public string BeamoId;
	public BeamoProtocolType? ProtocolType;

	public string[] ServiceDependencies;

	// HttpMicroservice args
	public string DockerBuildContext;
	public string DockerfileRelativePath;
	
	// Embedded Mongo args
	public string BaseImage;
}

public class ServicesRegisterCommand : AppCommand<ServicesRegisterCommandArgs>
{
	private readonly BeamoLocalService _localBeamo;

	public ServicesRegisterCommand(BeamoLocalService localBeamo) :
		base("register",
			"Registers a new service into the local manifest.")
	{
		_localBeamo = localBeamo;
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--id", "The Unique Id for this service within this Beamable CLI context."),
			(args, i) => args.BeamoId = i);

		AddOption(new Option<BeamoProtocolType?>("--type", () => null, $"The type of protocol this service implements: {string.Join(",", Enum.GetNames(typeof(BeamoProtocolType)))}"),
			(args, i) => args.ProtocolType = i);

		AddOption(new Option<string[]>("--deps", "The ','-separated list of existing Beam-O Ids that this service depends on."),
			(args, i) => args.ServiceDependencies = i.Length == 0 ? null : i);

		// For HttpProtocol
		{
			AddOption(new Option<string>("--build-context", "The path to a valid docker build context with a Dockerfile in it."),
				(args, i) => args.DockerBuildContext = i);
			AddOption(new Option<string>("--dockerfile", "The relative file path, from the given build-context, to a valid Dockerfile inside that context."),
				(args, i) => args.DockerfileRelativePath = i);
		}

		// For EmbeddedMongo Protocol
		{
			AddOption(new Option<string>("--base-image", () => null, "Name and tag of the base image to use for the local mongo db instance."),
				(args, i) => args.BaseImage = i);
		}
	}

	public override async Task Handle(ServicesRegisterCommandArgs args)
	{
		// Handle Beamo Id Option 
		{
			if (string.IsNullOrEmpty(args.BeamoId))
				args.BeamoId = AnsiConsole.Prompt(new TextPrompt<string>("Enter an unique identifier for this [lightskyblue1]Beam-O Container[/]:"));

			if (!_localBeamo.ValidateBeamoServiceId_ValidCharacters(args.BeamoId))
			{
				AnsiConsole.MarkupLine($"[red]\nBeam-O Ids can only contain alphanumeric and underscore characters.[/]");
				return;
			}

			if (!_localBeamo.ValidateBeamoServiceId_DoesntExists(args.BeamoId))
			{
				AnsiConsole.MarkupLine(
					$"[red]\nBeam-O Ids must be unique per-project. Here are the ones already taken: {string.Join(", ", _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId))}[/]");
				return;
			}
		}

		// Handle Type Option
		if (args.ProtocolType == null)
			args.ProtocolType = AnsiConsole.Prompt(new SelectionPrompt<BeamoProtocolType>()
				.Title("What [green]Beam-O Protocol Type[/] does the container respect?")
				.AddChoices(BeamoProtocolType.EmbeddedMongoDb, BeamoProtocolType.HttpMicroservice));

		// Handle Dependencies Option
		{
			if (args.ServiceDependencies == null)
				args.ServiceDependencies = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
					.Title("Service Dependencies")
					.NotRequired()
					.InstructionsText("Select any number of other Beam-O containers as dependencies of this one. We check for cyclical dependencies so don't worry.")
					.AddChoices(_localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId))
				).ToArray();

			if (args.ServiceDependencies.Length > 0 && !args.ServiceDependencies.All(dep => _localBeamo.IsServiceRegistered(dep)))
			{
				AnsiConsole.MarkupLine(
					$"[red]\nCannot depend on unregistered service. Here are the services already registered: {string.Join(", ", _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId))}[/]");
				return;
			}
		}

		switch (args.ProtocolType)
		{
			case BeamoProtocolType.HttpMicroservice:
			{
				// Handle DockerBuildContext
				{
					if (string.IsNullOrEmpty(args.DockerBuildContext))
						args.DockerBuildContext = AnsiConsole.Prompt(new TextPrompt<string>("Enter the relative path to a valid [lightskyblue1]Docker Build Context[/]:"));

					if (!Directory.Exists(args.DockerBuildContext))
					{
						AnsiConsole.MarkupLine($"[red]\nThe given path does not exist![/]");
						return;
					}
				}

				// Handle Dockerfile path
				{
					if (string.IsNullOrEmpty(args.DockerfileRelativePath))
						args.DockerfileRelativePath =
							AnsiConsole.Prompt(new TextPrompt<string>("Enter the [lightskyblue1]Dockerfile[/]'s path (from the given [lightskyblue1]Docker Build Context[/]'s root):"));

					var dockerfilePath = Path.Combine(args.DockerBuildContext, args.DockerfileRelativePath);
					if (!File.Exists(dockerfilePath))
					{
						AnsiConsole.MarkupLine($"[red]\nNo dockerfile found at path [{dockerfilePath}]![/]");
						return;
					}
				}

				await _localBeamo.AddDefinition_HttpMicroservice(args.BeamoId, args.DockerBuildContext, args.DockerfileRelativePath, args.ServiceDependencies, CancellationToken.None);
				// TODO: if type specific parameters happened update the definition
				break;
			}
			case BeamoProtocolType.EmbeddedMongoDb:
			{
				// Handle base image
				{
					if (string.IsNullOrEmpty(args.BaseImage))
						args.BaseImage = AnsiConsole.Prompt(new TextPrompt<string>("Enter the base image name of a [lightskyblue1]Mongo Db[/] image:").DefaultValue("mongo:latest"));

					await _localBeamo.AddDefinition_EmbeddedMongoDb(args.BeamoId, args.BaseImage, args.ServiceDependencies, CancellationToken.None);
				}
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
		}

		_localBeamo.SaveBeamoLocalManifest();
		_localBeamo.SaveBeamoLocalRuntime();
		
		await _localBeamo.StopListeningToDocker();
	}
}
