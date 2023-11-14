using JetBrains.Annotations;
using Serilog;
using System.CommandLine;

namespace cli;

public class GenerateSdkCommandArgs : CommandArgs
{
	public bool Concat;
	[CanBeNull] public string OutputPath;
	public string Filter;
	public string Engine;
	public GenerateSdkConflictResolutionStrategy ResolutionStrategy;
}

public enum GenerateSdkConflictResolutionStrategy
{
	None,
	RenameAllConflicts,
	RenameUncommonConflicts
}

public class GenerateSdkCommand : AppCommand<GenerateSdkCommandArgs>, IStandaloneCommand
{
	private SwaggerService _swagger;

	public GenerateSdkCommand() : base("generate", "Generate Beamable client source code from open API documents")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--concat", () => false,
			"When true, all the generated code will be in one file. When false, there will be multiple files"),
			(args, val) => args.Concat = val); // TODO: In C#, we can concat, but in C++/js, it could make no sense to support concat
		AddOption(new Option<string>("--output", () => null,
			"When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path"),
			(args, val) => args.OutputPath = val);

		AddOption(new Option<string>("--filter", () => null,
			"Filter which open apis to generate. An empty string matches everything"),
			(args, val) => args.Filter = val);

		AddOption(new Option<string>("--engine", () => "",
				$"Filter which engine code we should generate ({SwaggerService.TARGET_ENGINE_NAME_UNITY} | {SwaggerService.TARGET_ENGINE_NAME_UNREAL}). An empty string matches everything"),
			(args, val) => args.Engine = val);

		AddOption(new Option<GenerateSdkConflictResolutionStrategy>("--conflict-strategy", () => GenerateSdkConflictResolutionStrategy.None,
			"When multiple openAPI documents identify a schema with the same name, this flag controls how the conflict is resolved"),
			(args, val) => args.ResolutionStrategy = val);
	}

	public override async Task Handle(GenerateSdkCommandArgs args)
	{
		_swagger = args.SwaggerService;

		var filter = BeamableApiFilter.Parse(args.Filter);
		var output = await _swagger.Generate(filter, args.Engine, args.ResolutionStrategy);

		var outputData = !string.IsNullOrEmpty(args.OutputPath);
		// TODO: rewrite as a pattern match
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
			var file = new GeneratedFileDescriptor
			{
				FileName = args.OutputPath,
				Content = string.Join("\n", output.Select(o => o.Content))
			};
			output = new List<GeneratedFileDescriptor> { file };
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
