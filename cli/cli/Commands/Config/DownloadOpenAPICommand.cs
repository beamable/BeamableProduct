using JetBrains.Annotations;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Serilog;
using System.CommandLine;

namespace cli;

public class DownloadOpenAPICommandArgs : CommandArgs
{
	[CanBeNull] public string OutputPath;
	public string Filter;
	public bool CombineIntoOneDocument;

	public bool OutputToStandardOutOnly => string.IsNullOrWhiteSpace(OutputPath);
}


public class DownloadOpenAPICommand : AppCommand<DownloadOpenAPICommandArgs>, IEmptyResult
{
	private SwaggerService _swaggerService;

	public DownloadOpenAPICommand() : base("download", "Download the Beamable Open API specs")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--output", () => null,
				"When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path"),
			(args, val) => args.OutputPath = val);

		AddOption(new Option<string>("--filter", () => null,
				"Filter which open apis to generate. An empty string matches everything"),
			(args, val) => args.Filter = val);
		AddOption(new ConfigurableOptionFlag("combine-into-one-document",
				"Combines all API documents into one."),
			(args, val) => args.CombineIntoOneDocument = val);
	}

	public override async Task Handle(DownloadOpenAPICommandArgs args)
	{
		_swaggerService = args.SwaggerService;
		// TODO: download the files to a folder...
		var filter = BeamableApiFilter.Parse(args.Filter);

		var data = await _swaggerService.DownloadBeamableApis(filter);

		if (args.CombineIntoOneDocument)
		{
			var combined = _swaggerService.GetCombinedDocument(data);
			var json = combined.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

			if (args.OutputToStandardOutOnly)
			{
				Log.Information(json);
				return;
			}
			var pathName = Path.Combine(args.OutputPath!, "combinedOpenApi.json");

			Directory.CreateDirectory(Path.GetDirectoryName(pathName) ?? throw new InvalidOperationException());
			await File.WriteAllTextAsync(pathName, json);
			return;
		}

		foreach (var api in data)
		{
			var json = api.Document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

			if (args.OutputToStandardOutOnly)
			{
				Log.Information(json);
				continue;
			}

			var pathName = Path.Combine(args.OutputPath!, api.Descriptor.FileName);

			Directory.CreateDirectory(Path.GetDirectoryName(pathName) ?? throw new InvalidOperationException());
			await File.WriteAllTextAsync(pathName, json);
		}
	}
}
