using System.CommandLine;

namespace cli;

public class CidOption : ConfigurableOption
{
	public static CidOption Instance { get; } = new CidOption();

	private CidOption()
		: base(Constants.CONFIG_CID, "Cid to use; will default to whatever is in the file system")
	{ }
}
