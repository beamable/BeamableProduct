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
		var killed = new HashSet<int>();
		var stoppedAny = false;

		// 1) Kill the recorded pid's tree. On unix this is the exec'd leaf; on Windows `up` resolved it to the
		//    real service leaf (the JVM) so killing it directly works even after the cmd/powershell wrappers die.
		if (entry.pid > 0)
		{
			killed.Add(entry.pid);
			stoppedAny |= KillPid(entry.pid, entry.name);
		}

		// 2) Fallback: the Windows wrapper chain (cmd → powershell → java, cmd → npm → node, cmd → dotnet) can
		//    die and orphan the real runtime, leaving the recorded pid dead/reused. Find the runtime by a
		//    stack-specific identity string on its command line and kill it. Also self-heals runtimes orphaned
		//    by older CLI builds that recorded only the wrapper pid. Strictly token-gated so unrelated
		//    java/node/dotnet processes (Rider, MSBuild, MCP) are never touched. No-op on non-Windows.
		foreach (var pid in LocalStackProcess.FindByCommandLine(BuildKillTokens(entry), LocalStackProcess.ServiceImages))
		{
			if (!killed.Add(pid))
				continue;
			if (KillPid(pid, entry.name))
			{
				stoppedAny = true;
				Log.Information($"[{entry.name}] stopped orphaned pid={pid} (matched by command line)");
			}
		}

		if (!stoppedAny)
			Log.Information($"[{entry.name}] already stopped (pid={entry.pid})");

		return true;
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
