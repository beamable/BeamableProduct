namespace cli;

public class HostOption : ConfigurableOption
{
	public HostOption() : base(Constants.CONFIG_PLATFORM, "This option defines the target Beamable environment. Needed for private cloud customers to target their exclusive Beamable environment." +
	                                                      $" Ignorable by everyone else. Stored in '.beamable/{Constants.CONFIG_DEFAULTS_FILE_NAME}'")
	{
	}
}
