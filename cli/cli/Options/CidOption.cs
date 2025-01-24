using System.CommandLine;

namespace cli;

public class CidOption : ConfigurableOption
{
	public static CidOption Instance { get; } = new CidOption();

	private CidOption()
		: base(Constants.CONFIG_CID, $"CID (CustomerId) to use (found in Portal->Account); defaults to whatever is in '.beamable/{Constants.CONFIG_DEFAULTS_FILE_NAME}'")
	{ }
}
