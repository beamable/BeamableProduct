using Beamable.Common;
using Beamable.Server.Generator;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli;

public class GenerateClientOapiCommand : AtomicCommand<GenerateClientOapiCommandArgs, GenerateClientOapiCommandArgsResult>, ISkipManifest
{
	public GenerateClientOapiCommand() : base("generate-client-oapi", "Generate Client Code from OAPI specifications")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--output-dir", "Directory to write the output client at"), (arg, i) => arg.outputDirectory = i);
	}

	public override async Task Handle(GenerateClientOapiCommandArgs args)
	{
		await args.BeamoLocalSystem.InitManifest();
		await base.Handle(args);
	}

	public override Task<GenerateClientOapiCommandArgsResult> GetResult(GenerateClientOapiCommandArgs args)
	{
		var result = new GenerateClientOapiCommandArgsResult();
		foreach (var openApiDoc in args.BeamoLocalSystem.BeamoManifest.MicroserviceOpenApiSpecifications)
		{
			var generator = new OpenApiClientCodeGenerator(openApiDoc);
			if (!string.IsNullOrEmpty(args.outputDirectory))
			{
				Directory.CreateDirectory(args.outputDirectory);
				var outputPath = Path.Combine(args.outputDirectory, $"{openApiDoc.Info.Title}Client.cs");
				generator.GenerateCSharpCode(outputPath);
				result.outputsPaths.Add(outputPath);
			}
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
}
