using System.CommandLine;

namespace cli;

public class PidOption : Option<string>
{
	public PidOption()
		:base("--pid", "a pid to use; will default to whatever is in the file system")
	{ }
}
