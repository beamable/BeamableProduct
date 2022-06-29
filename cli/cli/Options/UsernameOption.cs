using System.CommandLine;

namespace cli;

public class UsernameOption : Option<string>
{
	public UsernameOption()
		: base("--username", "a user name")
	{ }
}
