using System.CommandLine;

namespace cli;

public class PasswordOption : Option<string>, IAmRequiredForRedirection
{
	public PasswordOption()
		: base("--password", "User password")
	{

	}

}
