using System.CommandLine;

namespace cli.Options;

public class QuietOption : Option<bool>
{
	public static QuietOption Instance { get; } = new QuietOption();
	private QuietOption() : base("--quiet", () => false, "When true, skip input waiting and use default arguments (or error if no defaults are possible)")
	{
		AddAlias("-q");
	}
}
