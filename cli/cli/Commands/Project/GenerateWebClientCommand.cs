using Beamable.Common.Semantics;
using Beamable.Server;
using cli.Services;
using cli.Services.Web;
using cli.Services.Web.Helpers;
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
	private const string Int64AsOptionName = "--int64-as";
	private const string BuildOptionName = "--build";

	public override void Configure()
	{
		AddOption(new Option<string>(OutputDirAliases, "The directory where the generated code will be written"),
			(arg, i) => arg.outputDirectory = i);

		AddOption(
			new Option<string>(LangAliases, () => "typescript",
				"The language of the generated code. Valid values are: `typescript` (default), `ts`, `javascript`, `js`"),
			(arg, i) => arg.lang = i);

		AddOption(
			new Option<string>(Int64AsOptionName, () => "bigint-union",
				"How C# `long` (OpenAPI int64) fields are typed in the generated TypeScript. Valid values are: `bigint-union` (default, `bigint | string`), `number`, `string`"),
			(arg, i) => arg.int64As = i);

		// Opt-in rather than default-on: building runs `dotnet build` for every microservice, which is slow,
		// needs a working toolchain, and would turn an unrelated compile error into a codegen failure.
		AddOption(
			new Option<bool>(BuildOptionName, () => false,
				"Build the microservices first so the generated clients match the current code, instead of using the OpenAPI documents from their last build"),
			(arg, i) => arg.build = i);
	}

	public override async Task Handle(GenerateWebClientCommandArgs args)
	{
		// Fail on bad arguments before doing any (potentially slow) manifest or build work.
		ValidateArgs(args);

		await args.BeamoLocalSystem.InitManifest();

		if (args.build)
		{
			await BuildServices(args);

			// The OpenAPI documents are read while the manifest is generated, so re-read them after the build.
			await args.BeamoLocalSystem.InitManifest(useManifestCache: false);
		}

		await base.Handle(args);
	}

	public override Task<GenerateWebClientCommandArgsResult> GetResult(GenerateWebClientCommandArgs args)
	{
		var int64Mapping = ValidateArgs(args);
		var result = Generate(args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols,
			args.outputDirectory, args.lang, int64Mapping);
		return Task.FromResult(result);
	}

	/// <summary>
	/// Throws a <see cref="CliException"/> for a missing <c>--output-dir</c>, an unsupported <c>--lang</c> or an
	/// unsupported <c>--int64-as</c> value.
	/// </summary>
	/// <returns>The parsed <c>--int64-as</c> value.</returns>
	public static TsInt64Mapping ValidateArgs(GenerateWebClientCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.outputDirectory))
		{
			throw new CliException(
				$"Missing required option {OutputDirAliases[0]}. Pass the directory to write the generated clients to, e.g. `{OutputDirAliases[0]} web/src`.");
		}

		if (string.IsNullOrWhiteSpace(args.lang) || !VALID_LANGUAGES.Contains(args.lang.ToLowerInvariant()))
		{
			throw new CliException(
				$"Unsupported language type: [{args.lang}]. Valid values for {LangAliases[0]} are: {string.Join(", ", VALID_LANGUAGES)}.");
		}

		var int64As = string.IsNullOrWhiteSpace(args.int64As) ? "bigint-union" : args.int64As;
		if (!OpenApiTsTypeMapper.TryParseInt64Mapping(int64As, out var int64Mapping))
		{
			throw new CliException(
				$"Unsupported {Int64AsOptionName} value: [{args.int64As}]. Valid values are: {string.Join(", ", OpenApiTsTypeMapper.Int64MappingNames)}.");
		}

		return int64Mapping;
	}

	/// <summary>
	/// Generates one client per microservice that has an OpenAPI document (and, for TypeScript, the shared types
	/// file) under <c>{outputDirectory}/beamable/clients</c>. Services without a document are skipped with a
	/// warning; if no client can be generated at all, this throws a <see cref="CliException"/>.
	/// </summary>
	public static GenerateWebClientCommandArgsResult Generate(
		IEnumerable<KeyValuePair<string, HttpMicroserviceLocalProtocol>> services,
		string outputDirectory,
		string lang,
		TsInt64Mapping int64Mapping)
	{
		var result = new GenerateWebClientCommandArgsResult();
		var clientsOutputDirectory = Path.Combine(outputDirectory, "beamable/clients");
		var generators = new List<WebClientCodeGenerator>();
		var skippedServices = new List<string>();

		foreach ((string beamoId, HttpMicroserviceLocalProtocol localProtocol) in services)
		{
			var openApiDoc = localProtocol.OpenApiDoc;
			if (openApiDoc == null)
			{
				skippedServices.Add(beamoId);
				var expectedPath = string.IsNullOrEmpty(localProtocol.ExpectedOpenApiDocPath)
					? "its build output folder"
					: localProtocol.ExpectedOpenApiDocPath;
				Log.Warning(
					$"Skipping web client generation for service=[{beamoId}] because it has no OpenAPI document. Expected it at [{expectedPath}]. " +
					$"Build the service with `beam project build --ids {beamoId}` (or pass {BuildOptionName}) and run this command again.");
				continue;
			}

			var generator = new WebClientCodeGenerator(openApiDoc, lang, int64Mapping: int64Mapping);
			generators.Add(generator);
			result.Clients.Add(generator.GenerateClientCode(clientsOutputDirectory));
		}

		if (generators.Count == 0)
		{
			if (skippedServices.Count == 0)
			{
				throw new CliException(
					"No web clients were generated because there are no microservices in this workspace.");
			}

			throw new CliException(
				$"No web clients were generated because none of the microservices have an OpenAPI document: [{string.Join(", ", skippedServices)}]. " +
				$"Build them with `beam project build` (or pass {BuildOptionName}) and run this command again.");
		}

		if (!WebClientCodeGenerator.IsTypeScriptLanguage(lang))
		{
			return result;
		}

		var typesOutputDirectory = Path.Combine(clientsOutputDirectory, "types");
		var clientTypeFilePath = WebClientCodeGenerator.GenerateClientTypes(typesOutputDirectory, generators);
		if (!string.IsNullOrEmpty(clientTypeFilePath))
		{
			result.Types.Add(clientTypeFilePath);
		}

		return result;
	}

	private static async Task BuildServices(GenerateWebClientCommandArgs args)
	{
		var failedServices = new List<string>();
		foreach (var beamoId in args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.Keys.ToList())
		{
			if (!args.BeamoLocalSystem.VerifyCanBeBuiltLocally(beamoId))
			{
				Log.Warning($"Skipping the build of service=[{beamoId}] because it cannot be built locally.");
				continue;
			}

			Log.Information($"Building service=[{beamoId}] before generating web clients...");
			var subArgs = args.Create<BuildProjectCommandArgs>();
			await ProjectService.WatchBuild(subArgs, new ServiceName(beamoId),
				ProjectService.BuildFlags.DisableClientCodeGen, report =>
				{
					if (report.isSuccess)
					{
						return;
					}

					failedServices.Add(beamoId);
					Log.Error($"Build of service=[{beamoId}] failed:\n" +
					          string.Join("\n", report.errors.Select(e => e.formattedMessage)));
				});
		}

		if (failedServices.Count > 0)
		{
			throw new CliException(
				$"Could not generate web clients because the build failed for: [{string.Join(", ", failedServices)}]. Fix the build errors above and run this command again.");
		}
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
	public string int64As;
	public bool build;
}
