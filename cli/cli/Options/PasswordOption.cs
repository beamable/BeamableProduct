using System.CommandLine;

namespace cli;

public class PasswordOption : Option<string>
{
	public PasswordOption()
		: base("--password", "User password")
	{

	}

}
