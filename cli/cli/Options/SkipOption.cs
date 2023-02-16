using System.CommandLine;

namespace cli;

public class SkipOption : Option<int>
{
	public SkipOption() : base("--skip", "Skip specified amount of manifests")
	{ }
}
