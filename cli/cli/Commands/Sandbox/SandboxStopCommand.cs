using System.Linq;
using Beamable.Server;
using cli.Services.Sandbox;

namespace cli.Sandbox;

public class SandboxStopCommandArgs : CommandArgs
{
}

public class SandboxStopCommandResult
{
	public List<string> stoppedServices = new();
	public string message = string.Empty;
}

public class SandboxStopCommand : AtomicCommand<SandboxStopCommandArgs, SandboxStopCommandResult>
{
	public SandboxStopCommand() : base("stop", "Stop the running sandbox for this account in this realm")
	{
	}

	public override void Configure()
	{
	}

	public override Task<SandboxStopCommandResult> GetResult(SandboxStopCommandArgs args)
	{
		// TODO: we can re-use the existing discovery stuff that `beam project ps` uses
		// v1 skeleton: drop the local code files for sandboxes the current user launched on
		// this machine. The remote Shutdown route call (account + HMAC) lands once the MS
		// wire-up is in place; until then this still cleans up local artifacts so `start`
		// can re-issue without confusion.
		var state = new SandboxStateService();
		var stopped = new List<string>();
		foreach (var (serviceName, _) in state.ListLocalSandboxes().ToList())
		{
			state.RemoveCode(serviceName);
			stopped.Add(serviceName);
		}

		var message = stopped.Count switch
		{
			0 => "No local sandboxes to stop.",
			1 => $"Removed local state for {stopped[0]}.",
			_ => $"Removed local state for {stopped.Count} sandboxes."
		};

		return Task.FromResult(new SandboxStopCommandResult
		{
			stoppedServices = stopped,
			message = message,
		});
	}
}
