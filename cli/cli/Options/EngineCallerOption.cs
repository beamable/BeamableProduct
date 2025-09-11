namespace cli.Options;

public class EngineCallerOption : ConfigurableOption
{
	public static EngineCallerOption Instance { get; } = new EngineCallerOption();

	private EngineCallerOption() : base("engine", "If passed, sets the engine integration that is calling for the command")
	{
	}
}
