namespace cli;

public class HostOption : ConfigurableOption
{
	public HostOption() : base(Constants.CONFIG_PLATFORM, "The host endpoint for beamable")
	{
	}
}
