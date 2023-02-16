using System.CommandLine;

namespace cli;

public class PidOption : ConfigurableOption
{
	public PidOption()
		: base(Constants.CONFIG_PID, "Pid to use; will default to whatever is in the file system")
	{ }
}
