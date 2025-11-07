using JetBrains.Annotations;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using System.CommandLine;
using Beamable.Server;
using cli.Services;

namespace cli;

public class DownloadOpenAPICommandArgs : CommandArgs
{
	[CanBeNull] public string OutputPath;
	public string Filter;
	public bool CombineIntoOneDocument;

	public bool OutputToStandardOutOnly => string.IsNullOrWhiteSpace(OutputPath);
}


public class DownloadOpenAPICommand : AppCommand<DownloadOpenAPICommandArgs>, IEmptyResult, IStandaloneCommand
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
				"Combines all API documents into one. In order to achieve that it will need to rename some of the types because of duplicates, eg. GetManifestResponse"),
			(args, val) => args.CombineIntoOneDocument = val);
	}

	public override async Task Handle(DownloadOpenAPICommandArgs args)
	{
		_swaggerService = args.SwaggerService;
		// TODO: download the files to a folder...
		var filter = BeamableApiFilter.Parse(args.Filter);

		var documents = await _swaggerService.DownloadBeamableApis(filter);

		if (args.CombineIntoOneDocument)
		{
			var data = SwaggerService.ExtractAllSchemas(documents.Select(result => result.Document).ToList(),
				GenerateSdkConflictResolutionStrategy.RenameUncommonConflicts);
			var combined = _swaggerService.GetCombinedDocument(data);

			combined.Info.Title = "Beamable API";
			combined.ExternalDocs.Url = new Uri("https://help.beamable.com");
			
			var json = combined.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

			if (args.OutputToStandardOutOnly)
			{
				Log.Information(json);
				return;
			}

			if (string.IsNullOrEmpty(args.OutputPath))
			{
				args.OutputPath = "beam-oapi.json";
			}
			var dir = Path.GetDirectoryName(args.OutputPath);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			await File.WriteAllTextAsync(args.OutputPath, json);
			return;
		}

		foreach (var api in documents)
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
