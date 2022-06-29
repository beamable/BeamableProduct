namespace cli;

public class PlatformOption : ConfigurableOption
{
	public PlatformOption() : base(Constants.CONFIG_PLATFORM, "The host endpoint for beamable.")
	{
	}
}
