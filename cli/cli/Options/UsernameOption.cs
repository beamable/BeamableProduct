using System.CommandLine;
using System.CommandLine.Invocation;

namespace cli;

public class UsernameOption : Option<string>, IAmRequiredForRedirection
{
	public UsernameOption()
		: base("--email", "Specify user email address")
	{
		AddAlias("--username"); // exists for legacy reasons, because this option used to indicate username, but we've ALWAYS required an email. 
	}
}
