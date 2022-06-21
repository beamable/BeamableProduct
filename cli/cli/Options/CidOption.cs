using System.CommandLine;

namespace cli;

public class CidOption : ConfigurableOption
{
	public CidOption()
		:base(Constants.CONFIG_CID, "a cid to use; will default to whatever is in the file system")
	{ }
}
