namespace cli;

public class CliException : Exception
{
	public bool UseNonZeroExitCode;
	public CliException(string message) : base(message) { }

	public CliException(string message, bool useNonZeroExitCode) : base(message)
	{
		UseNonZeroExitCode = useNonZeroExitCode;
	}
}
