namespace cli;

public class HostOption : ConfigurableOption
{
	public static HostOption Instance { get; } = new HostOption();
	private HostOption() : base(ConfigService.CFG_JSON_FIELD_HOST, "This option defines the target Beamable environment. Needed for private cloud customers to target their exclusive Beamable environment." +
	                                                      $" Ignorable by everyone else. Stored in '.beamable/{ConfigService.CFG_FILE_NAME}'")
	{
	}
}
