using System.CommandLine;

namespace cli;

public class EnableReporterOption : Option<bool>
{
	public EnableReporterOption()
		: base("--reporter-use-fatal", "Allows calls made to the reporter server to be logged on the FATAL channel.")
	{
		this.IsHidden = true;
	}
}
