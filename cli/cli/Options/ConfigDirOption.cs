using System.CommandLine;

namespace cli;

public class ConfigDirOption : ConfigurableOption
{
	public ConfigDirOption()
		: base(Constants.CONFIG_PID, "the directory to use for configuration")
	{ }
}
