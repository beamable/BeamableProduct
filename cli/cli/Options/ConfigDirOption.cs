using System.CommandLine;

namespace cli;

public class ConfigDirOption : ConfigurableOption
{
	public ConfigDirOption()
		: base(Constants.CONFIG_DIR, "[DEPRECATED] Path override for the .beamable folder")
	{ }
}
