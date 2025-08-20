using JetBrains.Annotations;

namespace cli.OtelCommands;

public class OtelCommand : CommandGroup
{
	public OtelCommand() : base("otel", "Allows access to Open Telemetry related commands")
	{
	}
}
