using System.CommandLine;

namespace cli;

public class CidOption : ConfigurableOption
{
	public static CidOption Instance { get; } = new CidOption();

	private CidOption()
		: base(ConfigService.CFG_JSON_FIELD_CID, $"CID (CustomerId) to use (found in Portal->Account); defaults to whatever is in '.beamable/{ConfigService.CFG_FILE_NAME}'")
	{ }
}
