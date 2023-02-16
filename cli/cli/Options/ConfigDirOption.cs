using System.CommandLine;

namespace cli;

public class ConfigDirOption : ConfigurableOption
{
	public ConfigDirOption()
		: base(Constants.CONFIG_DIR, "Directory to use for configuration")
	{ }
}
