using Beamable.Server;
using cli.Services.LocalStack;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Commands.LocalStack;

public class LocalStackStopCommandArgs : CommandArgs
{
	public string configPath;
	public string step;

	/// <summary>Reverse docker steps destructively (<c>compose down -v</c>) instead of just stopping the
	/// containers — deletes the local database along with them.</summary>
	public bool purge;
}

public class LocalStackStopCommandResult
{
	public string[] stopped;
	public int remaining;
}

/// <summary>
/// Stops the local stack recorded by <c>beam local up</c>. Long-running processes are killed
/// (whole process tree, in reverse start order); run-to-completion steps that declared a
/// <c>stopArguments</c> reversal (e.g. <c>docker compose stop</c>) are reversed. With a step name, only
/// that step is stopped; otherwise the whole stack is stopped and the run-state cleared.
/// <para>
/// Stopping is NON-DESTRUCTIVE by default: docker steps are reversed with <c>compose stop</c>, so the
/// containers and their volumes survive and the next <c>up</c> reuses the existing database. Pass
/// <c>--purge</c> to use each step's destructive reversal (<c>compose down -v</c>) instead, which removes
/// the containers and volumes — that deletes the local accounts/customers/realms, so the next <c>up</c>
/// seeds a brand-new realm under a new CID.
/// </para>
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
		AddOption(new Option<bool>(new[] { "--purge", "--clean" },
				"DESTRUCTIVE: reverse docker steps with `compose down -v` (removing the containers and their "
				+ "volumes) instead of `compose stop`. This deletes the local database — accounts, customers "
				+ "and realms — so the next `up` seeds a brand-new realm with a new CID; omit to keep data"),
			(args, v) => args.purge = v);
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
		var forget = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (var i = targeting.Count - 1; i >= 0; i--)
		{
			var entry = targeting[i];
			var outcome = StopEntry(entry, args.purge);
			if (outcome == StopOutcome.Stopped)
				stopped.Add(entry.name);

			// Forget a step once it is down (or was already down). A step we could neither confirm nor stop stays
			// recorded, so `ps`/`stop` can still act on it instead of it becoming an untracked orphan.
			if (outcome != StopOutcome.Unconfirmed)
				forget.Add(entry.name);
		}

		// Update the run-state: drop the steps that are down; delete the file when nothing remains.
		state.steps.RemoveAll(s => forget.Contains(s.name));
		if (state.steps.Count == 0)
		{
			LocalStackRunStateIO.Clear(runStatePath);
			// Whole stack down — remove this run's temp log dir (kept when the user passed --save-logs).
			if (state.ephemeralLogs && !string.IsNullOrEmpty(state.logsDir))
			{
				try { if (Directory.Exists(state.logsDir)) Directory.Delete(state.logsDir, recursive: true); }
				catch (Exception e) { Log.Verbose($"could not delete temp log dir {state.logsDir}: {e.Message}"); }
			}
		}
		else
			LocalStackRunStateIO.Save(runStatePath, state);

		Log.Information($"Stopped {stopped.Count} step(s). {state.steps.Count} still recorded.");
		return Task.FromResult(new LocalStackStopCommandResult
		{
			stopped = stopped.ToArray(),
			remaining = state.steps.Count
		});
	}

	/// <summary>What happened to one step: whether it was brought down, was already down, or could neither be
	/// confirmed nor stopped (in which case it must stay recorded rather than be forgotten).</summary>
	private enum StopOutcome
	{
		Stopped,
		AlreadyGone,
		Unconfirmed
	}

	private static StopOutcome StopEntry(LocalStackRunEntry entry, bool purge)
	{
		// Reversible run-to-completion steps (e.g. docker compose up -d) are reversed via their stop command.
		var reversal = ResolveReversal(entry, purge);
		if (!string.IsNullOrWhiteSpace(reversal) && !string.IsNullOrWhiteSpace(entry.command))
			return RunReversal(entry, reversal) ? StopOutcome.Stopped : StopOutcome.Unconfirmed;

		return KillTree(entry);
	}

	/// <summary>
	/// Picks a step's reversal arguments: the destructive <see cref="LocalStackRunEntry.purgeStopArguments"/>
	/// when <paramref name="purge"/> is set and the step declares one, otherwise the non-destructive
	/// <see cref="LocalStackRunEntry.stopArguments"/>. Run-states recorded before <c>purgeStopArguments</c>
	/// existed carry only the latter, so <c>--purge</c> falls back to whatever they recorded.
	/// </summary>
	private static string ResolveReversal(LocalStackRunEntry entry, bool purge) =>
		purge && !string.IsNullOrWhiteSpace(entry.purgeStopArguments)
			? entry.purgeStopArguments
			: entry.stopArguments;

	private static bool RunReversal(LocalStackRunEntry entry, string stopArguments)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = entry.command,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			foreach (var a in stopArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				psi.ArgumentList.Add(a);
			if (!string.IsNullOrEmpty(entry.workingDirectory) && Directory.Exists(entry.workingDirectory))
				psi.WorkingDirectory = entry.workingDirectory;

			var proc = Process.Start(psi);
			proc?.WaitForExit();
			Log.Information($"[{entry.name}] reversed via `{entry.command} {stopArguments}`");
			return true;
		}
		catch (Exception e)
		{
			Log.Warning($"[{entry.name}] failed to reverse (`{entry.command} {stopArguments}`): {e.Message}");
			return false;
		}
	}

	private static StopOutcome KillTree(LocalStackRunEntry entry)
	{
		var killed = new HashSet<int>();
		var stoppedAny = false;
		var tokens = BuildKillTokens(entry);
		var liveness = LocalStackLiveness.Check(entry, tokens);

		// 1) Kill the recorded pid's tree. On unix this is the exec'd leaf; on Windows `up` resolved it to the
		//    real service leaf (the JVM) so killing it directly works even after the cmd/powershell wrappers die.
		//    Only when the pid is CONFIRMED to be this service: a recorded pid can have been recycled by an
		//    unrelated process since the stack was started (observed: a dead `scala: auth` pid reused by an audio
		//    service), and killing whatever now holds that number would take out a bystander.
		if (entry.pid > 0 && liveness == LocalStackLiveness.Liveness.Running)
		{
			killed.Add(entry.pid);
			stoppedAny |= KillPid(entry.pid, entry.name);
		}
		else if (entry.pid > 0)
		{
			Log.Verbose($"[{entry.name}] recorded pid {entry.pid} is {liveness} — not killing it by pid");
		}

		// 2) Fallback: the Windows wrapper chain (cmd → powershell → java, cmd → npm → node, cmd → dotnet) can
		//    die and orphan the real runtime, leaving the recorded pid dead/reused. Find the runtime by a
		//    stack-specific identity string on its command line and kill it. Also self-heals runtimes orphaned
		//    by older CLI builds that recorded only the wrapper pid. Strictly token-gated so unrelated
		//    java/node/dotnet processes (Rider, MSBuild, MCP) are never touched. No-op on non-Windows.
		foreach (var pid in LocalStackProcess.FindByCommandLine(tokens, LocalStackProcess.ServiceImages))
		{
			if (!killed.Add(pid))
				continue;
			if (KillPid(pid, entry.name))
			{
				stoppedAny = true;
				Log.Information($"[{entry.name}] stopped orphaned pid={pid} (matched by command line)");
			}
		}

		if (stoppedAny)
			return StopOutcome.Stopped;

		// Nothing was killed. If the pid is confirmed gone the step is simply already down; but if it may still be
		// alive and we could not confirm it, saying "already stopped" and forgetting it would leave a process
		// running (holding its port) with nothing tracking it — so keep the entry and say so.
		if (liveness == LocalStackLiveness.Liveness.Stopped)
		{
			Log.Information($"[{entry.name}] already stopped (pid={entry.pid})");
			return StopOutcome.AlreadyGone;
		}

		Log.Warning($"[{entry.name}] could not confirm or stop pid {entry.pid} — leaving it recorded. " +
		            "If it is still running, stop it by hand (it may still hold its port).");
		return StopOutcome.Unconfirmed;
	}

	/// <summary>Kills a process and its tree. Returns true if a live process was actually killed.</summary>
	private static bool KillPid(int pid, string name)
	{
		try
		{
			var proc = Process.GetProcessById(pid);
			if (proc.HasExited)
				return false;
			proc.Kill(entireProcessTree: true);
			return true;
		}
		catch (ArgumentException)
		{
			return false; // no process with that id — already gone
		}
		catch (Exception e)
		{
			Log.Warning($"[{name}] failed to stop pid={pid}: {e.Message}");
			return false;
		}
	}

	/// <summary>
	/// Builds the command-line identity tokens used to find an orphaned runtime whose recorded pid is stale.
	/// Tokens are chosen per step kind so they are <em>specific to that one step</em> (never shared across
	/// steps, so <c>stop &lt;step&gt;</c> stays precise) and specific to this stack (so unrelated
	/// java/node/dotnet processes are never matched):
	/// <list type="bullet">
	/// <item><c>shell</c> (Scala JVM): the <c>mainClass</c> and the service's <c>tools/&lt;svc&gt;/</c>
	/// classpath fragment — NOT the shared Scala repo working directory.</item>
	/// <item><c>process</c> (C# gateway exe, portal node/vite): the per-step working directory, an absolute
	/// path that appears on the runtime's command line and is unique to the step.</item>
	/// <item><c>beam</c> (microservice/extension/group): the service id, which appears on the beam runner's
	/// <c>--ids &lt;id&gt;</c>/<c>--with-group &lt;id&gt;</c> and on the child runtime's <c>&lt;id&gt;.dll</c>.</item>
	/// </list>
	/// </summary>
	public static List<string> BuildKillTokens(LocalStackRunEntry entry)
	{
		var tokens = new List<string>();
		if (entry == null)
			return tokens;

		if (string.Equals(entry.kind, "shell", StringComparison.OrdinalIgnoreCase))
		{
			if (!string.IsNullOrWhiteSpace(entry.matchToken))
				tokens.Add(entry.matchToken.Trim());

			var svc = DeriveSuffix(entry.name, "scala:");
			if (!string.IsNullOrEmpty(svc))
			{
				tokens.Add($"tools/{svc}/");
				tokens.Add($"tools\\{svc}\\");
			}
		}
		else if (string.Equals(entry.kind, "process", StringComparison.OrdinalIgnoreCase))
		{
			if (!string.IsNullOrWhiteSpace(entry.workingDirectory))
				tokens.Add(entry.workingDirectory.Trim());
		}
		else if (string.Equals(entry.kind, "beam", StringComparison.OrdinalIgnoreCase))
		{
			foreach (var prefix in new[] { "microservice:", "portal extension:", "group:" })
			{
				var id = DeriveSuffix(entry.name, prefix);
				if (!string.IsNullOrEmpty(id))
				{
					tokens.Add(id);
					break;
				}
			}
		}

		return tokens;
	}

	/// <summary>Returns the text after <paramref name="prefix"/> in <paramref name="stepName"/> (case- and
	/// space-insensitive), or null when the prefix is absent or nothing follows it.</summary>
	public static string DeriveSuffix(string stepName, string prefix)
	{
		if (string.IsNullOrWhiteSpace(stepName) || string.IsNullOrEmpty(prefix))
			return null;

		var idx = stepName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
			return null;

		var suffix = stepName.Substring(idx + prefix.Length).Trim();
		return string.IsNullOrEmpty(suffix) ? null : suffix;
	}
}
