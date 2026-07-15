using System.Threading;
using System.Threading.Tasks;

namespace cli.Services.Sandbox;

/// <summary>
/// Abstraction over "actually run a CLI command line". Implementations capture output
/// via <see cref="IInvocationSink"/> and report terminal status. The observer is the
/// single point that wires invocations to the notification batcher; the runner only
/// sees the sink, so a runner is unit-testable against a fake sink in isolation from
/// the rest of the sandbox.
/// </summary>
public interface IInvocationRunner
{
	Task RunAsync(Invocation invocation, IInvocationSink sink, CancellationToken token);
}

public interface IInvocationSink
{
	void EmitOutput(string channel, string line);
	void EmitStatus(InvocationStatusKind status, int? exitCode = null, string? failureReason = null);
}
