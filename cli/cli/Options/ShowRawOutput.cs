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

public class EmitLogsOption : Option<bool>
{
	public static readonly EmitLogsOption Instance = new EmitLogsOption();
	private EmitLogsOption()
		: base("--emit-log-streams", "Out all log messages as data payloads in addition to however they are logged")
	{

	}
}

public class NoForwardingOption : Option<bool>
{
	public const string OPTION_FLAG = "--no-redirect";
	public static NoForwardingOption Instance = new NoForwardingOption();
	private NoForwardingOption()
		: base(
			name: OPTION_FLAG,
			description: "If there is a local dotnet tool installation (with a ./config/dotnet-tools.json file) for the beam tool, " +
						 "then any global invocation of the beam tool will automatically redirect and call the local version. " +
						 "However, there will be a performance penalty due to the extra process invocation. This option flag will " +
						 "cause an error to occur instead of automatically redirecting the execution to a new process invocation. ")
	{

	}
}
