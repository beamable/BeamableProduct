using System.CommandLine;

namespace cli;

public class DryRunOption : Option<bool>
{
	public DryRunOption()
		: base("--dryrun", "should any networking happen?")
	{

	}
}
