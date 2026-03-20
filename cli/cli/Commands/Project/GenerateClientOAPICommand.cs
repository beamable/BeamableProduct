using Beamable.Common;
using Beamable.Server.Generator;
using cli.Services;
using Microsoft.OpenApi.Readers;
using System.CommandLine;
using Beamable.Server;
using cli.Dotnet;

namespace cli;

public class GenerateClientOapiCommand : AtomicCommand<GenerateClientOapiCommandArgs, GenerateClientOapiCommandArgsResult>, ISkipManifest
{
	public GenerateClientOapiCommand() : base("generate-client-oapi", "Generate Client Code from OAPI specifications")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) =>
		{
			args.beamoIds = i;
		});
		AddOption(new Option<bool>("--server", "Instructs the generator to write code for a server, not a client"),
			(args, i) => args.forServer = i);
		AddOption(new Option<string>("--output-dir", "Directory to write the output client at"), (arg, i) => arg.outputDirectory = i);
		AddOption(new Option<string>("--oapi-path", "Path to an existing beam_openApi.json file; skips manifest lookup when provided"),
			(args, i) => args.oapiPath = i);
	}

	public override async Task Handle(GenerateClientOapiCommandArgs args)
	{
		// Skip manifest loading when a direct OAPI path is provided
		if (string.IsNullOrEmpty(args.oapiPath))
		{
			await args.BeamoLocalSystem.InitManifest();
		}
		await base.Handle(args);
	}

	public override Task<GenerateClientOapiCommandArgsResult> GetResult(GenerateClientOapiCommandArgs args)
	{
		var result = new GenerateClientOapiCommandArgsResult();
		if (string.IsNullOrEmpty(args.outputDirectory))
		{
			return Task.FromResult(result);
		}

		// Fast path: caller supplied a direct path to a beam_openApi.json file
		if (!string.IsNullOrEmpty(args.oapiPath))
		{
			GenerateFromFile(args.oapiPath, args.outputDirectory, args.forServer, result);
			return Task.FromResult(result);
		}

		// Manifest path: look up by beamo id(s)
		ProjectCommand.FinalizeServicesArg(args, ref args.beamoIds, true);

		BeamoLocalManifest beamoLocalManifest = args.BeamoLocalSystem.BeamoManifest;
		foreach ((string beamoId, HttpMicroserviceLocalProtocol localProtocol) in beamoLocalManifest.HttpMicroserviceLocalProtocols)
		{
			if (!args.beamoIds.Contains(beamoId)) continue;

			var openApiDoc = localProtocol.OpenApiDoc;
			if (openApiDoc == null)
			{
				Log.Warning($"Skipping client-gen for {beamoId} because no open-api doc was found. Please build the service first and try again.");
				continue;
			}

			var outputPath = WriteClient(openApiDoc, args.outputDirectory, args.forServer);
			result.outputsPaths.Add(outputPath);
		}

		return Task.FromResult(result);
	}

	static void GenerateFromFile(string oapiPath, string outputDirectory, bool forServer, GenerateClientOapiCommandArgsResult result)
	{
		if (!File.Exists(oapiPath))
		{
			Log.Warning($"Skipping client-gen because the OAPI file was not found at: {oapiPath}");
			return;
		}

		var reader = new OpenApiStringReader();
		var openApiDoc = reader.Read(File.ReadAllText(oapiPath), out var diagnostic);
		foreach (var error in diagnostic.Errors)
		{
			throw new CliException($"Invalid OAPI document at {oapiPath}: {error.Message} ({error.Pointer})");
		}

		var outputPath = WriteClient(openApiDoc, outputDirectory, forServer);
		result.outputsPaths.Add(outputPath);
	}

	static string WriteClient(Microsoft.OpenApi.Models.OpenApiDocument openApiDoc, string outputDirectory, bool forServer)
	{
		Directory.CreateDirectory(outputDirectory);
		var outputPath = Path.Combine(outputDirectory, $"{openApiDoc.Info.Title}Client.cs");

		if (forServer)
		{
			var generator = new OpenApiServerCodeGenerator(openApiDoc);
			generator.GenerateCSharpCode(outputPath);
		}
		else
		{
			var generator = new OpenApiClientCodeGenerator(openApiDoc);
			generator.GenerateCSharpCode(outputPath);
		}

		return outputPath;
	}
}


public class GenerateClientOapiCommandArgsResult
{
	public List<string> outputsPaths = new();
}

public class GenerateClientOapiCommandArgs : CommandArgs
{
	public string outputDirectory;
	public List<string> beamoIds;
	public bool forServer;
	public string oapiPath;
}
