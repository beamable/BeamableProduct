using JetBrains.Annotations;
using Serilog;
using System.CommandLine;

namespace cli;

public class GenerateSdkCommandArgs : CommandArgs
{
	public bool Concat;
	public CleaningStrategy CleaningStrategy;
	[CanBeNull] public string OutputPath;
	public string Filter;
	public string Engine;
	public GenerateSdkConflictResolutionStrategy ResolutionStrategy;
}

public enum CleaningStrategy
{
	RemoveDir,
	RemoveCsFiles,
	DoNotClean,
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
			(args, val) =>
				args.Concat =
					val); // TODO: In C#, we can concat, but in C++/js, it could make no sense to support concat
		AddOption(new Option<string>("--output", () => null,
				"When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path"),
			(args, val) => args.OutputPath = val);

		AddOption(new Option<string>("--filter", () => null,
				"Filter which open apis to generate. An empty string matches everything"),
			(args, val) => args.Filter = val);

		AddOption(new Option<string>("--engine", () => "",
				$"Filter which engine code we should generate ({SwaggerService.TARGET_ENGINE_NAME_UNITY} | {SwaggerService.TARGET_ENGINE_NAME_UNREAL}). An empty string matches everything"),
			(args, val) => args.Engine = val);

		AddOption(new Option<GenerateSdkConflictResolutionStrategy>("--conflict-strategy",
				() => GenerateSdkConflictResolutionStrategy.None,
				"When multiple openAPI documents identify a schema with the same name, this flag controls how the conflict is resolved"),
			(args, val) => args.ResolutionStrategy = val);

		AddOption(new Option<CleaningStrategy>("--cleaning-strategy", () => CleaningStrategy.RemoveDir,
				"Specifies what should happened with directory output"),
			(args, val) => args.CleaningStrategy = val);
	}

	public override async Task Handle(GenerateSdkCommandArgs args)
	{
		_swagger = args.SwaggerService;

		var filter = BeamableApiFilter.Parse(args.Filter);
		var output = await _swagger.Generate(filter, args.Engine, args.ResolutionStrategy);

		var outputData = !string.IsNullOrEmpty(args.OutputPath);
		if (outputData)
		{
			switch (args.OutputPath.EndsWith(".cs"), args.Concat)
			{
				case (false, true):
					throw new CliException("when concat is enabled, output path must end in .cs");
				case (true, false):
					throw new CliException("when concat is disabled, output path must be a directory");
				default:
					break;
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

		// Make it a clean generation every time.
		if (outputData)
		{
			switch (args.CleaningStrategy)
			{
				case CleaningStrategy.RemoveDir when args.Engine == SwaggerService.TARGET_ENGINE_NAME_UNREAL:
				{
					// We always clean up the output directory's AutoGen folder  --- every file we create is in the AutoGen folder.
					var outputDirInfo = new DirectoryInfo(args.OutputPath);
					var autoGenDirs = outputDirInfo.GetDirectories("AutoGen", SearchOption.AllDirectories);
					foreach (DirectoryInfo directoryInfo in autoGenDirs)
					{
						// We don't clean up the CLI Autogen folder in this command.
						if (directoryInfo.Parent!.ToString().Contains("CLI")) continue;
						Directory.Delete(directoryInfo.ToString(), true);
					}
					break;
				}
				case CleaningStrategy.RemoveDir:
				{
					Directory.Delete(args.OutputPath, true);
					break;
				}
				case CleaningStrategy.RemoveCsFiles when !args.Concat:
				{
					var files = Directory.GetFiles(args.OutputPath, "*.cs", SearchOption.AllDirectories);
					foreach (var file in files)
					{
						File.Delete(file);
					}

					break;
				}
				case CleaningStrategy.DoNotClean:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
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
