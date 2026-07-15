using System.Threading;
using System.Threading.Tasks;

namespace cli.Services.Sandbox;

/// <summary>
/// Phase 2A placeholder. Emits a couple of canned output lines and completes with exit 0
/// so Portal can exercise the full Invoke → notifications → re-attach contract without
/// the real CLI runner. Phase 2B replaces this with an in-process runner that drives
/// the actual <c>System.CommandLine</c> command tree (see <c>ServerService.HandleExec</c>
/// for the pattern to mirror).
/// </summary>
public sealed class StubInvocationRunner : IInvocationRunner
{
	public async Task RunAsync(Invocation invocation, IInvocationSink sink, CancellationToken token)
	{
		sink.EmitOutput("stream", $"[stub] would run: {invocation.CommandLine}");
		await Task.Delay(50, token);
		sink.EmitOutput("logs", "[stub] real runner integration lands in Phase 2B");
		sink.EmitStatus(InvocationStatusKind.Completed, exitCode: 0);
	}
}
