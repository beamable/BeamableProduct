namespace cli;

public class CliException : Exception
{
	public bool UseNonZeroExitCode;
	public bool ReportOnStdOut;
	public CliException(string message) : base(message) { }

	public CliException(string message, bool useNonZeroExitCode, bool useStdOut) : base(message)
	{
		UseNonZeroExitCode = useNonZeroExitCode;
		ReportOnStdOut = useStdOut;
	}
}
