using Beamable.Common.BeamCli;
using Errata;
using System.Diagnostics;

namespace cli;

public class CliException<T> : CliException
	where T : ErrorOutput
{
	public T payload = default;

	protected override ErrorOutput Payload => payload;

	public static string GetChannelName() => GetChannelName(typeof(T));


	public CliException(string message) : base(message)
	{
	}

	public CliException(string message, int nonZeroOrOneExitCode, bool useStdOut, string additionalNote = null, IEnumerable<Diagnostic> additionalReports = null) : base(message, nonZeroOrOneExitCode, useStdOut, additionalNote, additionalReports)
	{
	}
}


public class CliException : Exception
{
	/// <summary>
	/// These are known and expected exceptions that will throw known and expected error codes.
	/// 
	/// </summary>
	public int NonZeroOrOneExitCode;

	public bool ReportOnStdOut;

	public List<Diagnostic> Reports;

	protected virtual ErrorOutput Payload { get; } = new ErrorOutput();

	public static string GetChannelName(Type payloadType)
	{
		if (payloadType == typeof(ErrorOutput))
		{
			// the "base" type is essentially an uncaught error until we map them over to special types. 
			return DefaultErrorStream.CHANNEL;
		}
		return "error" + payloadType.Name;
	}


	public CliException(string message) : base(message)
	{
		NonZeroOrOneExitCode = 1;
		ReportOnStdOut = true;
	}

	public CliException(string message, int nonZeroOrOneExitCode, bool useStdOut, string additionalNote = null, IEnumerable<Diagnostic> additionalReports = null) : base(message)
	{
		NonZeroOrOneExitCode = nonZeroOrOneExitCode;
		ReportOnStdOut = useStdOut;

		var baseReport = Diagnostic.Error(message)
			.WithCode($"{nonZeroOrOneExitCode:0000.##}");
		if (!string.IsNullOrWhiteSpace(additionalNote))
		{
			baseReport.WithNote(additionalNote);
		}
		Reports = new List<Diagnostic>() { baseReport };
		if (additionalReports != null)
		{
			Reports.AddRange(additionalReports);
		}

		Debug.Assert(NonZeroOrOneExitCode > 1, "NonZeroOrOneExitCode must be > 1 --- 0 is OK and 1 is \"private exception\" (not meant to be exposed to our cli-interface layer). In Engine, we check for these exceptions and ask for a bug report.");
	}

	public static void Apply(Exception ex, ref ErrorOutput output, int exitCode, string invocationContext)
	{
		output.exitCode = exitCode;
		output.invocation = invocationContext;
		output.message = ex.Message;
		output.fullTypeName = ex.GetType().FullName;
		output.stackTrace = ex.StackTrace;
		output.typeName = ex.GetType().Name;
	}


	public ErrorOutput GetPayload(int exitCode, string invocationContext)
	{
		var payload = Payload ?? new ErrorOutput();
		Apply(this, ref payload, exitCode, invocationContext);
		return payload;
	}
}
