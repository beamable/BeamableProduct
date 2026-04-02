using System.CommandLine;

namespace cli;

public class HostOption : ConfigurableOption
{
	public static HostOption Instance { get; } = new HostOption();
	private HostOption() : base(ConfigService.CFG_JSON_FIELD_HOST, "This option defines the target Beamable environment. Needed for private cloud customers to target their exclusive Beamable environment." +
	                                                      $" Ignorable by everyone else. Stored in '.beamable/{ConfigService.CFG_FILE_NAME}'")
	{
	}
}

public class PortalUrlOption : Option<string>
{
	public static PortalUrlOption Instance { get; } = new PortalUrlOption();
	private PortalUrlOption() : base("--portal-url", "Overrides the default portal url.")
	{
		IsHidden = true;
	}
}
