using Beamable.Server;
using cli.Services.LocalStack;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Commands.LocalStack;

public class LocalStackStopCommandArgs : CommandArgs
{
	public string configPath;
	public string step;
}

public class LocalStackStopCommandResult
{
	public string[] stopped;
	public int remaining;
}

/// <summary>
/// Stops the local stack recorded by <c>beam local up</c>. Long-running processes are killed
/// (whole process tree, in reverse start order); run-to-completion steps that declared a
/// <c>stopArguments</c> reversal (e.g. <c>docker compose down</c>) are reversed. With a step name, only
/// that step is stopped; otherwise the whole stack is stopped and the run-state cleared.
/// </summary>
public class LocalStackStopCommand
	: AtomicCommand<LocalStackStopCommandArgs, LocalStackStopCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackStopCommand() : base("stop", "Stop the running local stack (or a single step)")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("step", () => null, "The step name to stop (default: the whole stack)"),
			(args, v) => args.step = v);
		AddOption(new Option<string>("--config", "Path to the manifest whose run-state to read (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
	}

	public override Task<LocalStackStopCommandResult> GetResult(LocalStackStopCommandArgs args)
	{
		var runStatePath = LocalStackCommand.ResolveRunStatePath(args.ConfigService, args.configPath);
		var state = LocalStackRunStateIO.Load(runStatePath);
		if (state == null || state.steps.Count == 0)
			throw new CliException($"No running local stack recorded at {runStatePath}. Nothing to stop.");

		var targeting = string.IsNullOrWhiteSpace(args.step)
			? state.steps.ToList()
			: state.steps.Where(s => string.Equals(s.name, args.step, StringComparison.OrdinalIgnoreCase)).ToList();

		if (targeting.Count == 0)
			throw new CliException($"No recorded step named '{args.step}'. Known steps: {string.Join(", ", state.steps.Select(s => s.name))}");

		// Reverse start order: services before the infrastructure they depend on.
		var stopped = new List<string>();
		for (var i = targeting.Count - 1; i >= 0; i--)
		{
			var entry = targeting[i];
			if (StopEntry(entry))
				stopped.Add(entry.name);
		}

		// Update the run-state: drop the stopped steps; delete the file when nothing remains.
		var stoppedSet = new HashSet<string>(targeting.Select(s => s.name));
		state.steps.RemoveAll(s => stoppedSet.Contains(s.name));
		if (state.steps.Count == 0)
			LocalStackRunStateIO.Clear(runStatePath);
		else
			LocalStackRunStateIO.Save(runStatePath, state);

		Log.Information($"Stopped {stopped.Count} step(s). {state.steps.Count} still recorded.");
		return Task.FromResult(new LocalStackStopCommandResult
		{
			stopped = stopped.ToArray(),
			remaining = state.steps.Count
		});
	}

	private static bool StopEntry(LocalStackRunEntry entry)
	{
		// Reversible run-to-completion steps (e.g. docker compose up -d) are reversed via their stop command.
		if (!string.IsNullOrWhiteSpace(entry.stopArguments) && !string.IsNullOrWhiteSpace(entry.command))
			return RunReversal(entry);

		return KillTree(entry);
	}

	private static bool RunReversal(LocalStackRunEntry entry)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = entry.command,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			foreach (var a in entry.stopArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				psi.ArgumentList.Add(a);
			if (!string.IsNullOrEmpty(entry.workingDirectory) && Directory.Exists(entry.workingDirectory))
				psi.WorkingDirectory = entry.workingDirectory;

			var proc = Process.Start(psi);
			proc?.WaitForExit();
			Log.Information($"[{entry.name}] reversed via `{entry.command} {entry.stopArguments}`");
			return true;
		}
		catch (Exception e)
		{
			Log.Warning($"[{entry.name}] failed to reverse (`{entry.command} {entry.stopArguments}`): {e.Message}");
			return false;
		}
	}

	private static bool KillTree(LocalStackRunEntry entry)
	{
		if (entry.pid <= 0)
			return false;

		try
		{
			var proc = Process.GetProcessById(entry.pid);
			if (proc.HasExited)
			{
				Log.Information($"[{entry.name}] already stopped (pid={entry.pid})");
				return true;
			}

			proc.Kill(entireProcessTree: true);
			Log.Information($"[{entry.name}] stopped (pid={entry.pid})");
			return true;
		}
		catch (ArgumentException)
		{
			// No process with that id — already gone.
			Log.Information($"[{entry.name}] already stopped (pid={entry.pid})");
			return true;
		}
		catch (Exception e)
		{
			Log.Warning($"[{entry.name}] failed to stop (pid={entry.pid}): {e.Message}");
			return false;
		}
	}
}
