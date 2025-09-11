namespace cli.Options;

public class EngineSdkVersionOption : ConfigurableOption
{
	public static EngineSdkVersionOption Instance { get; } = new EngineSdkVersionOption();
	public EngineSdkVersionOption() : base("engine-sdk-version", "The version of the Beamable's SDK running in that Engine")
	{
	}
}
