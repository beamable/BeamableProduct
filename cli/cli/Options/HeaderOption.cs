using System.CommandLine;

namespace cli;

public class HeaderOption : ConfigurableOption
{
	public HeaderOption() : 
		base(Constants.CONFIG_HEADER, "Custom header")
	{ }
}
