using System.Diagnostics;

namespace cli;

public class CliException : Exception
{
	/// <summary>
	/// These are known and expected exceptions that will throw known and expected error codes.
	/// 
	/// </summary>
	public int NonZeroOrOneExitCode;

	public bool ReportOnStdOut;

	public CliException(string message) : base(message)
	{
		NonZeroOrOneExitCode = 1;
		ReportOnStdOut = true;
	}

	public CliException(string message, int nonZeroOrOneExitCode, bool useStdOut) : base(message)
	{
		NonZeroOrOneExitCode = nonZeroOrOneExitCode;
		ReportOnStdOut = useStdOut;
		Debug.Assert(NonZeroOrOneExitCode > 1, "NonZeroOrOneExitCode must be > 1 --- 0 is OK and 1 is \"private exception\" (not meant to be exposed to our cli-interface layer). In Engine, we check for these exceptions and ask for a bug report.");
	}
}
