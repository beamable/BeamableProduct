using JetBrains.Annotations;
using System.CommandLine.Help;

namespace cli.OtelCommands;


public class CollectorCommand : CommandGroup
{
	public CollectorCommand() : base("collector", "Allows access to otel collector related commands")
	{
	}
}
