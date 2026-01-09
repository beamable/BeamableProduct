using Beamable.Common;
using Beamable.Server.Generator;
using cli.Services;
using Newtonsoft.Json;
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
		AddOption(new Option<string>("--output-dir", "Directory to write the output client at"), (arg, i) => arg.outputDirectory = i);
	}

	public override async Task Handle(GenerateClientOapiCommandArgs args)
	{
		await args.BeamoLocalSystem.InitManifest();
		await base.Handle(args);
	}

	public override Task<GenerateClientOapiCommandArgsResult> GetResult(GenerateClientOapiCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.beamoIds, true);
		
		var result = new GenerateClientOapiCommandArgsResult();
		if (string.IsNullOrEmpty(args.outputDirectory))
		{
			return Task.FromResult(result);
		}

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
			var generator = new OpenApiClientCodeGenerator(openApiDoc);

			Directory.CreateDirectory(args.outputDirectory);
			var outputPath = Path.Combine(args.outputDirectory, $"{openApiDoc.Info.Title}Client.cs");
			generator.GenerateCSharpCode(outputPath);
			result.outputsPaths.Add(outputPath);
		}

		return Task.FromResult(result);
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
}
