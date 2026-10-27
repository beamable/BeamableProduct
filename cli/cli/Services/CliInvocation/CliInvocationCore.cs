using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Server;

namespace cli.Services.CliInvocation;

/// <summary>
/// One CLI command run inside a long-lived process (`beam server serve`'s /execute, the MCP tools), so every
/// transport registers it, frames its failures and cancels it the same way.
/// </summary>
public class CliInvocationRequest
{
	/// <summary>
	/// Produces the command line and the entry listed in <see cref="CliInvocationCore.Snapshot"/> while it runs.
	/// It may throw (for example on a malformed request body); that is reported like any other setup failure.
	/// </summary>
	public Func<Task<(string commandLine, string registryEntry)>> ReadCommand;

	/// <summary>When true, the command line is split with the CLI's own splitter, which keeps JSON arguments intact.</summary>
	public bool UseCustomSplitter;

	/// <summary>Creates the reporter the command's output goes to, from the command's own service provider.</summary>
	public Func<IDependencyProvider, IDataReporterService> CreateReporter;

	/// <summary>
	/// Delivers a failure that happened before <see cref="CreateReporter"/> was called, so there is no reporter
	/// to send it through. Clients still need it as a regular error data point.
	/// </summary>
	public Func<ReportDataPoint<ErrorOutput>, Task> ReportSetupFailure;

	/// <summary>Cancels the command's <see cref="AppLifecycle"/> when the caller gives up.</summary>
	public CancellationToken CancellationToken;

	/// <summary>The app to run in; a fresh <see cref="App"/> when null. Tests pass a controlled one.</summary>
	public App App;

	/// <summary>
	/// Optional check once the app is built and before the command starts: a reason to refuse it, or null to run it.
	/// A refusal is reported like any other failure.
	/// </summary>
	public Func<App, string, string> Refuse;
}

public static class CliInvocationCore
{
	private static readonly object _invocationsLock = new object();

	/// <summary>Registry entries of the commands running right now. Guard every access with <see cref="_invocationsLock"/>.</summary>
	internal static readonly List<string> Invocations = new List<string>();

	/// <summary>A copy of the commands running right now, safe to hand out.</summary>
	public static List<string> Snapshot()
	{
		lock (_invocationsLock)
		{
			return new List<string>(Invocations);
		}
	}

	/// <summary>
	/// Runs the command and returns its exit code. Never throws for a command failure: setup and execution
	/// errors are reported in-band, through the reporter or <see cref="CliInvocationRequest.ReportSetupFailure"/>,
	/// with a non-zero exit code. The command is listed in <see cref="Snapshot"/> only while it actually runs.
	/// </summary>
	public static async Task<int> Execute(CliInvocationRequest request)
	{
		string registryEntry = null;
		string commandLine = null;
		IDataReporterService reporter = null;
		var registered = false;
		int exitCode = -1;
		try
		{
			(commandLine, registryEntry) = await request.ReadCommand();
			if (string.IsNullOrWhiteSpace(commandLine))
			{
				throw new ArgumentException("An execute request must include a non-empty commandLine.");
			}
			Log.Trace("virtualizing " + commandLine);
			var app = request.App ?? new App();

			var sw = System.Diagnostics.Stopwatch.StartNew();
			app.Configure(builder =>
			{
				builder.Remove<IDataReporterService>();
				builder.AddSingleton<IDataReporterService>(provider =>
				{
					reporter = request.CreateReporter(provider);
					return reporter;
				});
			}, overwriteLogger: false);
			app.Build();
			sw.Stop();
			Log.Trace("build virtual app in " + sw.ElapsedMilliseconds);

			var refusal = request.Refuse?.Invoke(app, commandLine);
			if (refusal != null)
			{
				throw new CliException(refusal);
			}

			using var cancellation = request.CancellationToken.CanBeCanceled
				? request.CancellationToken.Register(() => app.CommandProvider.GetService<AppLifecycle>().Cancel())
				: default;

			lock (_invocationsLock)
			{
				Invocations.Add(registryEntry);
				registered = true;
			}
			exitCode = await app.RunWithSingleString(commandLine, request.UseCustomSplitter);
		}
		catch (Exception ex)
		{
			exitCode = ex is CliException cliException ? cliException.NonZeroOrOneExitCode : 1;
			Log.Error($"CLI EXEC FINISHED WITH FAIL MESSAGE=[{ex.Message}]");
			if (reporter != null)
			{
				// Use the same writer/lock as command output and the connection heartbeat.
				reporter.Exception(ex, exitCode, commandLine);
			}
			else
			{
				var error = new ErrorOutput();
				CliException.Apply(ex, ref error, exitCode, commandLine);
				await request.ReportSetupFailure(new ReportDataPoint<ErrorOutput>
				{
					type = DefaultErrorStream.CHANNEL, data = error,
					ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
				});
			}
		}
		finally
		{
			if (registered)
			{
				lock (_invocationsLock)
				{
					Invocations.Remove(registryEntry);
				}
			}

			Log.Trace($"CLI EXEC FINISHED WITH EXIT=[{exitCode}] REQ=[{commandLine}]");
		}

		return exitCode;
	}
}
