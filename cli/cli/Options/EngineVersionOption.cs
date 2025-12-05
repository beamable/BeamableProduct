namespace cli.Options;

public class EngineVersionOption : ConfigurableOption
{
	public static EngineVersionOption Instance { get; } = new EngineVersionOption();

	public EngineVersionOption() : base("engine-version", "The version of the engine that is calling the CLI")
	{
	}
}
