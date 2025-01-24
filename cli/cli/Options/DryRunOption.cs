using System.CommandLine;

namespace cli;

public class DryRunOption : Option<bool>
{
	public DryRunOption()
		: base("--dryrun", "[DEPRECATED] Run as much of the command as possible without making any network calls")
	{

	}
}
