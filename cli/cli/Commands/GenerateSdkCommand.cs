using Serilog;
using System.CommandLine;

namespace cli;

public class GenerateSdkCommandArgs : CommandArgs
{
	public bool Concat;
	public string? OutputPath;
}
public class GenerateSdkCommand : AppCommand<GenerateSdkCommandArgs>
{
	private readonly SwaggerService _swagger;

	public GenerateSdkCommand(SwaggerService swagger) : base("generate", "generate Beamable client source code from open API documents")
	{
		_swagger = swagger;
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--concat", () => false,
			"when true, all the generated code will be in one file. When false, there will be multiple files"),
			(args, val) => args.Concat = val);
		AddOption(new Option<string>("--output", () => null,
			"when null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path."),
			(args, val) => args.OutputPath = val);
	}

	public override async Task Handle(GenerateSdkCommandArgs args)
	{
		var output = await _swagger.Generate();

		var outputData = !string.IsNullOrEmpty(args.OutputPath);
		if (outputData)
		{
			var isFile = args.OutputPath.EndsWith(".cs");
			if (args.Concat)
			{
				// the output path needs to be a file.
				if (!isFile)
				{
					throw new CliException("when concat is enabled, output path must end in .cs");
				}
			}
			else
			{
				if (isFile)
				{
					throw new CliException("when concat is disabled, output path must be a directory");
				}
			}
		}

		if (args.Concat)
		{
			var file = new GeneratedFileDescriptors
			{
				FileName = args.OutputPath, Content = string.Join("\n", output.Select(o => o.Content))
			};
			output = new List<GeneratedFileDescriptors> { file };
		}

		foreach (var file in output)
		{
			if (outputData)
			{
				var pathName = Path.Combine(args.OutputPath, file.FileName);
				if (args.Concat)
				{
					pathName = args.OutputPath;
				}

				Directory.CreateDirectory(Path.GetDirectoryName(pathName));
				File.WriteAllText(pathName, file.Content);
			}
			else
			{
				Log.Warning(file.FileName);
				Log.Warning(file.Content);
			}
		}
	}
}
