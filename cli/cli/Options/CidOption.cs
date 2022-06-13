using System.CommandLine;

namespace cli;

public class CidOption : Option<string>
{
	public CidOption()
		:base("--cid", "a cid to use; will default to whatever is in the file system")
	{ }
}
