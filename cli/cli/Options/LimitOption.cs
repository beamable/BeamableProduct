using System.CommandLine;

namespace cli;

public class LimitOption : Option<int>
{
	public LimitOption() : base("--limit", "limits amount of manifests")
	{ }
}
