using JetBrains.Annotations;

namespace cli.OtelCommands;

public class OtelCommand : CommandGroup
{

	public const string OTEL_COMMANDS_LOCK_FILE = "otel_operation";
	
	public OtelCommand() : base("telemetry", "Allows access to Open Telemetry related commands")
	{
		AddAlias("otel");
		AddAlias("tel");
	}
}
