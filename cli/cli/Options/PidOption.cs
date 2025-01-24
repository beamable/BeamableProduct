using System.CommandLine;

namespace cli;

public class PidOption : ConfigurableOption
{
	public static PidOption Instance { get; } = new PidOption();

	private PidOption()
		: base(Constants.CONFIG_PID, $"PID (Realm ID) to use (found in Portal -> Games -> Any Realm's details); defaults to whatever is in '.beamable/{Constants.CONFIG_DEFAULTS_FILE_NAME}'")
	{
	}
}
