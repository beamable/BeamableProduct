using System;
using System.Threading;
using System.Threading.Tasks;

namespace cli.Services.Sandbox;

/// <summary>
/// Drives a sandbox <see cref="Invocation"/> by building a fresh <see cref="App"/>,
/// swapping in <see cref="SandboxReporterService"/> so the CLI's structured output flows
/// to the invocation sink, and running the command line via
/// <see cref="App.RunWithSingleString"/>. Mirrors the pattern in
/// <c>cli/cli/Services/HttpServer/ServerService.cs:HandleExec</c> — the well-trodden
/// in-process CLI invocation path — but routes output to notifications instead of an
/// HttpListenerResponse.
///
/// Each invocation gets its own App instance. Building an App is non-trivial; for v1 the
/// per-invocation cost is acceptable, and isolating App per invocation keeps DI scopes
/// from leaking between concurrent invocations.
/// </summary>
public sealed class RealInvocationRunner : IInvocationRunner
{
	public async Task RunAsync(Invocation invocation, IInvocationSink sink, CancellationToken token)
	{
		var app = new App();
		app.Configure(builder =>
		{
			builder.Remove<IDataReporterService>();
			builder.AddSingleton<IDataReporterService, SandboxReporterService>(
				provider => new SandboxReporterService(provider, sink, token));
		}, overwriteLogger: false);
		app.Build();

		int exitCode;
		try
		{
			exitCode = await app.RunWithSingleString(invocation.CommandLine, useCustomSplitter: true);
		}
		catch (OperationCanceledException)
		{
			sink.EmitStatus(InvocationStatusKind.Cancelled);
			return;
		}
		catch (Exception ex)
		{
			// Surface the exception to the sink and mark Failed. The observer's catch block
			// would also produce a Failed status, but recording it explicitly here gives
			// Portal a useful failureReason payload rather than a generic "runner threw".
			sink.EmitOutput("error", $"runner exception: {ex.GetType().Name}: {ex.Message}");
			sink.EmitStatus(InvocationStatusKind.Failed, failureReason: ex.Message);
			return;
		}

		// Non-zero exits aren't an exception — they're how CLI commands signal "this didn't
		// succeed but the runner ran cleanly". Surface the code via exitCode so Portal can
		// distinguish "command errored" from "runner crashed".
		var status = exitCode == 0 ? InvocationStatusKind.Completed : InvocationStatusKind.Failed;
		sink.EmitStatus(status, exitCode: exitCode);
	}
}
