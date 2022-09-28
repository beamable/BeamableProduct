using System.CommandLine;

namespace cli;

public class SkipOption : Option<int>
{
	public SkipOption() : base("--skip", "skip specified amount of manifests")
	{ }
}
