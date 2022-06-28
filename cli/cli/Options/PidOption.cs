using System.CommandLine;

namespace cli;

public class PidOption : ConfigurableOption
{
	public PidOption()
		:base(Constants.CONFIG_PID, "a pid to use; will default to whatever is in the file system")
	{ }
}
