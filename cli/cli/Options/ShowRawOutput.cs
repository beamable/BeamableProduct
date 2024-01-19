using System.CommandLine;

namespace cli;

public class ShowRawOutput : Option<bool>
{
	public ShowRawOutput()
		: base("--raw", "When enabled, machine-readable JSON will be logged to the console. Note, when the command is being piped, this option has no effect.")
	{
	}
}

public class ShowPrettyOutput : Option<bool>
{
	public ShowPrettyOutput()
		: base("--pretty", "When enabled, any available higher level console graphics will be displayed on the console. Note, this is the default behaviour, except when the command is being piped.")
	{
		
	}
}
