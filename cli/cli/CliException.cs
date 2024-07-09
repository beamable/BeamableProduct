using Beamable.Common.BeamCli;
using Errata;
using System.Diagnostics;

namespace cli;

public class CliException<T> : CliException
{
	public T payload;

	public CliException(T payload) : base($"{typeof(T).Name}")
	{
		this.payload = payload;
	}

	public CliException(string message, int nonZeroOrOneExitCode, bool useStdOut, string additionalNote = null, IEnumerable<Diagnostic> additionalReports = null) : base(message, nonZeroOrOneExitCode, useStdOut, additionalNote, additionalReports)
	{
	}

	public override object GetPayload(int exitCode, string invocationContext)
	{
		return payload;
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

	public virtual object GetPayload(int exitCode, string invocationContext)
	{
		return new ErrorOutput
		{
			exitCode = exitCode,
			invocation = invocationContext,
			message = Message,
			stackTrace = StackTrace,
			typeName = GetType().Name,
			fullTypeName = GetType().FullName
		};
	}
}
