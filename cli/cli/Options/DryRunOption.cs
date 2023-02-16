using System.CommandLine;

namespace cli;

public class DryRunOption : Option<bool>
{
	public DryRunOption()
		: base("--dryrun", "Should any networking happen?")
	{

	}
}
