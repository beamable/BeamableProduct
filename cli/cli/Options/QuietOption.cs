using System.CommandLine;

namespace cli.Options;

public class QuietOption : Option<bool>
{
	public QuietOption() : base("--quiet", () => false, "When true, skip input waiting and use defaults")
	{
		AddAlias("-q");
	}
}
