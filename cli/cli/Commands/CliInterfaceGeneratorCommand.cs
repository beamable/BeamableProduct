using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using cli.Services;
using cli.Unreal;
using cli.Utils;
using JetBrains.Annotations;
using Serilog;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace cli;

public class CliInterfaceGeneratorCommandArgs : CommandArgs
{
	[CanBeNull] public string OutputPath;
	public bool Concat;
	public string Engine;
}
public class CliInterfaceGeneratorCommand : AppCommand<CliInterfaceGeneratorCommandArgs>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	private readonly IDependencyProviderScope _commandScope;

	public CliInterfaceGeneratorCommand(IDependencyProviderScope commandScope) : base("generate-interface", "Generates C# code for interfacing with the CLI from Unity")
	{
		_commandScope = commandScope;
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--concat", () => false,
				"When true, all the generated code will be in one file. When false, there will be multiple files"),
			(args, val) => args.Concat = val);

		AddOption(new Option<string>("--output", () => null,
				"When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path"),
			(args, val) => args.OutputPath = val);

		AddOption(new Option<string>("--engine", () => "",
				"Filter which engine code we should generate (unity | unreal). An empty string matches everything"),
			(args, val) => args.Engine = val);

	}

	public override Task Handle(CliInterfaceGeneratorCommandArgs args)
	{
		var generatorContext = args.DependencyProvider.GetService<CliGenerator>().GetCliContext();

		// now we have all the beam commands and their call sites
		// proxy out to a generator... for now, its unity... but someday it'll be unity or unreal.
		args.Engine = string.IsNullOrEmpty(args.Engine)
			? AnsiConsole.Ask<SelectionPrompt<string>>("")
				.AddChoices("unity", "unreal")
				.AddBeamHightlight().Show(AnsiConsole.Console)
			: args.Engine;
		ICliGenerator generator = args.Engine.ToLower() switch
		{
			"unity" => args.DependencyProvider.GetService<UnityCliGenerator>(),
			"unreal" => args.DependencyProvider.GetService<UnrealCliGenerator>(),
			// ReSharper disable once NotResolvedInText
			_ => throw new ArgumentOutOfRangeException("Should be impossible!")
		};

		var output = generator.Generate(generatorContext);


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

		return Task.CompletedTask;
	}
}

public class BeamCommandDescriptor
{
	public string executionPath;
	public Command command;
	public BeamCommandDescriptor parent;
	public List<BeamCommandDescriptor> children = new List<BeamCommandDescriptor>();
	public bool hasValidOutput;
	public List<BeamCommandResultDescriptor> resultStreams = new List<BeamCommandResultDescriptor>();


	public string ExecutionPathAsCapitalizedStringWithoutBeam(string separater = "")
	{
		var words = executionPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (words.Length == 1) return "Beam";
		return string.Join(separater, words.Skip(1).Select(w => w.Capitalize()));
	}

	public string GetSlug()
	{
		var path = executionPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (path.Length == 1)
		{
			return "beam";
		}
		return string.Join("-", path.Skip(1));
	}
}

public class BeamCommandResultDescriptor
{
	public string channel;
	public Type runtimeType;
}
