using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using System.CommandLine.Invocation;

namespace cli.Services;

public class CliGenerator
{
	private readonly IDependencyProvider _provider;

	public readonly static HashSet<Type> CommandTypesToReject = new HashSet<Type>
	{
		typeof(ServicesGenerateLocalManifestCommand)
	};

	public CliGenerator(IDependencyProvider provider)
	{
		_provider = provider;
	}
	public CliGeneratorContext GetCliContext()
	{
		var ctx = _provider.GetService<InvocationContext>();

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

			allCommands.Add(curr);


			foreach (var subCommand in curr.command.Subcommands)
			{
				if (CommandTypesToReject.Contains(subCommand.GetType())) continue;

				var subBeamCommand = new BeamCommandDescriptor
				{
					executionPath = $"{curr.executionPath} {subCommand.Name}",
					command = subCommand,
					parent = curr,
					hasValidOutput = subCommand.GetType().IsAssignableTo(typeof(IEmptyResult))
				};
				curr.children.Add(subBeamCommand);


				// find result streams...
				var interfaces = subCommand.GetType().GetInterfaces();
				foreach (var interfaceType in interfaces)
				{
					if (!interfaceType.IsGenericType) continue;
					if (interfaceType.GetGenericTypeDefinition() != typeof(IResultSteam<,>)) continue;
					var genArgs = interfaceType.GetGenericArguments();

					var channelType = genArgs[0];
					var resultType = genArgs[1];

					subBeamCommand.hasValidOutput = true;
					subBeamCommand.resultStreams.Add(new BeamCommandResultDescriptor
					{
						channel = (Activator.CreateInstance(channelType) as IResultChannel)?.ChannelName,
						runtimeType = resultType
					});
				}

				queue.Enqueue(subBeamCommand);
			}
		}

		return new CliGeneratorContext { Root = rootBeamCommand, Commands = allCommands };
	}
}
