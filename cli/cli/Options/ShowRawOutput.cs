using System.CommandLine;

namespace cli;

public class ShowRawOutput : Option<bool>
{
	public ShowRawOutput()
		: base("--raw", "Output raw JSON to standard out. This happens by default when the command is being piped")
	{
	}
}

public class ShowPrettyOutput : Option<bool>
{
	public ShowPrettyOutput()
		: base("--pretty", "Output syntax highlighted box text. This happens by default when the command is not piped")
	{
		
	}
}
