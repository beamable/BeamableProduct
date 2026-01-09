using Beamable.Common;
using cli.Services;
using cli.Services.Web;
using System.CommandLine;

namespace cli.Commands.Project;

public class GenerateWebClientCommand :
	AtomicCommand<GenerateWebClientCommandArgs, GenerateWebClientCommandArgsResult>, ISkipManifest
{
	public GenerateWebClientCommand() : base("web-client",
		"Generate TypeScript/JavaScript Client Code for Microservices")
	{
	}

	private static readonly string[] VALID_LANGUAGES = { "typescript", "ts", "javascript", "js" };

	private static readonly string[] OutputDirAliases = { "--output-dir", "-o" };
	private static readonly string[] LangAliases = { "--lang", "-l" };

	public override void Configure()
	{
		AddOption(new Option<string>(OutputDirAliases, "The directory where the generated code will be written"),
			(arg, i) => arg.outputDirectory = i);

		AddOption(
			new Option<string>(LangAliases, () => "typescript",
				"The language of the generated code. Valid values are: `typescript` (default), `ts`, `javascript`, `js`"),
			(arg, i) => arg.lang = i);
	}

	public override async Task Handle(GenerateWebClientCommandArgs args)
	{
		await args.BeamoLocalSystem.InitManifest();
		await base.Handle(args);
	}

	public override Task<GenerateWebClientCommandArgsResult> GetResult(GenerateWebClientCommandArgs args)
	{
		var result = new GenerateWebClientCommandArgsResult();
		if (string.IsNullOrEmpty(args.outputDirectory))
			return Task.FromResult(result);

		// validate lang argument
		if (!VALID_LANGUAGES.Contains(args.lang.ToLower()))
		{
			BeamableLogger.Log($"Unsupported language type: {args.lang}");
			return Task.FromResult(result);
		}

		BeamoLocalManifest beamoLocalManifest = args.BeamoLocalSystem.BeamoManifest;
		foreach ((_, HttpMicroserviceLocalProtocol localProtocol) in beamoLocalManifest.HttpMicroserviceLocalProtocols)
		{
			var openApiDoc = localProtocol.OpenApiDoc;
			if (openApiDoc == null)
				continue;

			var generator = new WebClientCodeGenerator(openApiDoc, args.lang);
			var clientsOutputDirectory = Path.Combine(args.outputDirectory, "beamable/clients");
			var clientFilePath = generator.GenerateClientCode(clientsOutputDirectory);
			result.Clients.Add(clientFilePath);
		}

		if (!WebClientCodeGenerator.IsTypeScript)
			return Task.FromResult(result);

		var typesOutputDirectory = Path.Combine(args.outputDirectory, "beamable/clients/types");
		var clientTypeFilePath = WebClientCodeGenerator.GenerateClientTypes(typesOutputDirectory);
		if (string.IsNullOrEmpty(clientTypeFilePath))
			return Task.FromResult(result);

		result.Types.Add(clientTypeFilePath);
		return Task.FromResult(result);
	}
}

public class GenerateWebClientCommandArgsResult
{
	public List<string> Clients = new();
	public List<string> Types = new();
}

public class GenerateWebClientCommandArgs : CommandArgs
{
	public string outputDirectory;
	public string lang;
}
