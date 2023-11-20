using System.CommandLine;

namespace cli;

public class SkipStandaloneValidationOption : Option<bool>
{
	public SkipStandaloneValidationOption()
		: base("--skip-standalone-validation", "skips the check for commands that require beam config directories.")
	{
		IsHidden = true;
	}
}
