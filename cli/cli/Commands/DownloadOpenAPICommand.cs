using JetBrains.Annotations;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;

namespace cli;

public class DownloadOpenAPICommandArgs : CommandArgs
{
	[CanBeNull] public string OutputPath;
	public string Filter;
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
	}

	public override async Task Handle(DownloadOpenAPICommandArgs args)
	{
		_swaggerService = args.SwaggerService;
		// TODO: download the files to a folder...
		var filter = BeamableApiFilter.Parse(args.Filter);

		var data = await _swaggerService.DownloadBeamableApis(filter);

		var hasOutput = !string.IsNullOrEmpty(args.OutputPath);

		foreach (var api in data)
		{
			var json = api.Document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

			if (!hasOutput)
			{
				Log.Information(json);
				// args.Reporter.Report("output", json);
				continue;
			}

			var pathName = Path.Combine(args.OutputPath, api.Descriptor.FileName);

			Directory.CreateDirectory(Path.GetDirectoryName(pathName));
			File.WriteAllText(pathName, json);
		}
	}
}
