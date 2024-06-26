using System.CommandLine;

namespace cli.Options;


public class UnmaskLogsOption : Option<bool>
{
	public static readonly UnmaskLogsOption Instance = new UnmaskLogsOption();

	private UnmaskLogsOption() : base("--unmask-logs", "By default, logs will automatically mask tokens. However, when this option is enabled, tokens will be visible in their full text. This is a security risk.")
	{
	}
}

public class NoLogFileOption : Option<bool>
{
	private const string ENV_VAR = "BEAM_CLI_NO_FILE_LOG";
	public static readonly NoLogFileOption Instance = new NoLogFileOption();

	private NoLogFileOption() : base(
		name: "--no-log-file", 
		getDefaultValue: () => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR)),
		description: $"By default, logs are automatically written to a temp file so that they can be used in an error case. However, when this option is enabled, logs are not written. Also, if the {ENV_VAR} environment variable is set, no log file will be written. ")
	{
		
	}
	
	
}
