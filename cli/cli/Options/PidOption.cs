using System.CommandLine;

namespace cli;

public class PidOption : ConfigurableOption
{
	public static PidOption Instance { get; } = new PidOption();

	private PidOption()
		: base(ConfigService.CFG_JSON_FIELD_PID, $"PID (Realm ID) to use (found in Portal -> Games -> Any Realm's details); defaults to whatever is in '.beamable/{ConfigService.CFG_FILE_NAME}'")
	{
	}
}
