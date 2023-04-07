using Beamable.Common.Dependencies;
using cli.Services;
using cli.Unreal;
using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace cli;

public class CliInterfaceGeneratorCommandArgs : CommandArgs
{
	public string? OutputPath;
	public bool Concat;
}
public class CliInterfaceGeneratorCommand : AppCommand<CliInterfaceGeneratorCommandArgs>
{
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

	}

	public override Task Handle(CliInterfaceGeneratorCommandArgs args)
	{
		var ctx = args.DependencyProvider.GetService<InvocationContext>();
		
		var allCommands = new List<BeamCommandDescriptor>();
		var rootCommand = ctx.Parser.Configuration.RootCommand;
		var rootBeamCommand = new BeamCommandDescriptor
		{
			executionPath = "beam", // the root command is special and has the tool name, not the assembly name
			command = rootCommand
		};
		
		// traverse the tree and create more BeamCommands
		var queue = new Queue<BeamCommandDescriptor>();
		queue.Enqueue(rootBeamCommand);
		var safety = 99999; // if ever have more than 99999 commands, send help.
		while (safety-- > 0 && queue.Count > 0)
		{
			var curr = queue.Dequeue();
			if (curr.executionPath != "beam")
			{
				allCommands.Add(curr);
			}

			foreach (var subCommand in curr.command.Subcommands)
			{
				var subBeamCommand = new BeamCommandDescriptor
				{
					executionPath = $"{curr.executionPath} {subCommand.Name}", 
					command = subCommand
				};
				queue.Enqueue(subBeamCommand);
			}
		}
		
		// now we have all the beam commands and their call sites
		// proxy out to a generator... for now, its unity... but someday it'll be unity or unreal.
		var generator = args.DependencyProvider.GetService<ICliGenerator>();
		var output = generator.Generate(new CliGeneratorContext
		{
			Root = rootBeamCommand,
			Commands = allCommands
		});
		
		
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
		
		return Task.CompletedTask;
	}
}

public class BeamCommandDescriptor
{
	public string executionPath;
	public Command command;

	public string ExecutionPathAsCapitalizedStringWithoutBeam()
	{
		var words = executionPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return string.Join("", words.Skip(1).Select(w => w.Capitalize()));
	}
}
