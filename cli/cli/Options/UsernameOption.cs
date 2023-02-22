using System.CommandLine;

namespace cli;

public class UsernameOption : Option<string>
{
	public UsernameOption()
		: base("--username", "Specify user name")
	{ }
}
