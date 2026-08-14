using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.LocalStack;

/// <summary>
/// The record of a running local stack, written by <c>beam local up</c> and read by
/// <c>beam local ps</c> / <c>logs</c> / <c>stop</c>. It lives next to the manifest at
/// <c>&lt;dir&gt;/local-stack.run.json</c> and lets the lifecycle commands find the processes and log
/// files of a stack that was brought up (and detached) by a previous CLI invocation.
/// </summary>
public class LocalStackRunState
{
	public string host;
	public string portalUrl;

	/// <summary>The per-run directory holding this run's step logs (temp or workspace — see
	/// <see cref="LocalStackRunStateIO.ResolveRunLogsDir"/>). Recorded so <c>stop</c> can clean it up.</summary>
	public string logsDir;

	/// <summary>True when <see cref="logsDir"/> is a temp folder that <c>stop</c> should delete once the whole
	/// stack is down; false when the user passed <c>--save-logs</c> and the logs are kept under the workspace.</summary>
	public bool ephemeralLogs;

	/// <summary>The launched steps, in start order.</summary>
	public List<LocalStackRunEntry> steps = new List<LocalStackRunEntry>();
}

/// <summary>One launched step in a <see cref="LocalStackRunState"/>.</summary>
public class LocalStackRunEntry
{
	public string name;
	public string group;

	/// <summary>OS process id of the launched service. On unix this is the exec'd leaf; on Windows <c>up</c>
	/// resolves it to the real service grandchild (the JVM) once the step is ready — see
	/// <see cref="LocalStackProcess.ResolveLeafPid"/>.</summary>
	public int pid;

	/// <summary>
	/// UTC ticks of <see cref="pid"/>'s process start time, recorded at launch. This is what makes the pid
	/// trustworthy later: a pid alone can be recycled by an unrelated process, but pid + start time identifies
	/// one specific process. 0 for run-states written before this existed (liveness then falls back to checking
	/// the process image, see <see cref="LocalStackLiveness"/>).
	/// </summary>
	public long startedAtUtcTicks;

	/// <summary>Identity string present on the launched service's command line (e.g. the Scala
	/// <c>mainClass</c>). Used by <c>stop</c> as a fallback to find and kill a service whose recorded
	/// <see cref="pid"/> is stale — the Windows wrapper chain (<c>cmd → powershell → java</c>) dies when
	/// <c>up</c> returns and orphans the JVM.</summary>
	public string matchToken;

	/// <summary>process | beam | shell | docker — for display and stop semantics.</summary>
	public string kind;

	public string stdoutLog;
	public string stderrLog;
	public string workingDirectory;

	/// <summary>The executable used to launch (recorded so <c>stop</c> can reverse docker steps).</summary>
	public string command;

	/// <summary>If set, <c>stop</c> runs <c>command stopArguments</c> in <see cref="workingDirectory"/> to
	/// reverse a run-to-completion step (e.g. <c>compose stop</c>). Non-destructive: it runs on every plain
	/// <c>beam local stop</c>.</summary>
	public string stopArguments;

	/// <summary>If set, <c>stop --purge</c> runs this instead of <see cref="stopArguments"/> (e.g.
	/// <c>compose down -v</c>, which also deletes the containers' volumes and so the local database).</summary>
	public string purgeStopArguments;

	/// <summary>True for run-to-completion steps (e.g. <c>docker compose up -d</c>): their <see cref="pid"/>
	/// is expected to be dead; liveness is not judged by it.</summary>
	public bool waitForExit;

	/// <summary>
	/// True when <c>up</c> did not launch this step because it was already answering its readiness endpoint, and
	/// recorded it anyway so <c>stop</c> can bring it down. Such an entry has no <see cref="pid"/> (the owning
	/// process was never a child of this <c>up</c>) — <c>stop</c> finds it by <see cref="stopArguments"/> for
	/// docker steps and by the command-line token sweep for everything else. Purely informational for the
	/// stop/ps output; it changes no logic.
	/// </summary>
	public bool adopted;
}

/// <summary>
/// Decides whether a step recorded by a previous detached <c>up</c> is genuinely still running.
///
/// A live pid is NOT enough. Pids get recycled, and a recorded pid that now belongs to an unrelated process
/// makes <c>up</c> skip launching a service it believes is already up — observed for real: an audio service had
/// inherited a dead <c>scala: auth</c> pid, so <c>up</c> reported "already running (pid=6344) — skipping" and
/// the whole stack came up with no auth service at all (and <c>ps</c> agreed it was running). The live process
/// must therefore also look like the runtime the step actually launches.
/// </summary>
public static class LocalStackLiveness
{
	/// <summary>
	/// Process images the stack launches, as bare names (no extension, so this matches on every OS). Used only
	/// by the legacy fallback below; <c>beam</c>/<c>Beamable.Tools</c> are included because a <c>beam</c> step
	/// runs as the CLI's own apphost when it is a globally installed tool rather than <c>dotnet &lt;dll&gt;</c>.
	/// </summary>
	private static readonly string[] StackImages =
	{
		"java", "javaw", "node", "dotnet", "beam", "Beamable.Tools",
		"BeamableGateway", "BeamableMessageRailRuntime", "BeamableCampaignRuntime"
	};

	/// <summary>Tolerance when comparing a live process's start time to the recorded one (clock/rounding slack).</summary>
	private static readonly TimeSpan StartTimeSlack = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Whether a recorded step is still running. <see cref="Liveness.Unverified"/> is a distinct answer on
	/// purpose: "we could not tell" must never be silently promoted to "yes, still running" (that is how a
	/// recycled pid got treated as a live service), nor to "no" (that would relaunch a service that is in fact
	/// up). Each caller picks the safe side for what it is about to do.
	/// </summary>
	public enum Liveness
	{
		/// <summary>The recorded process is confirmed alive.</summary>
		Running,

		/// <summary>The recorded process is confirmed gone (or the pid now belongs to something else).</summary>
		Stopped,

		/// <summary>The pid is alive and plausible, but its identity could not be confirmed.</summary>
		Unverified
	}

	/// <summary>
	/// Resolves <paramref name="entry"/>'s liveness. Pass the step's identity tokens (see
	/// <c>LocalStackStopCommand.BuildKillTokens</c>) so a legacy run-state (no recorded start time) can be checked
	/// against the pid's command line.
	/// </summary>
	public static Liveness Check(LocalStackRunEntry entry, IEnumerable<string> identityTokens)
	{
		if (entry == null || entry.waitForExit || entry.pid <= 0)
		{
			return Liveness.Stopped;
		}

		Process process;
		try
		{
			process = Process.GetProcessById(entry.pid);
			if (process.HasExited)
			{
				return Liveness.Stopped;
			}
		}
		catch
		{
			return Liveness.Stopped; // no such pid
		}

		// The exact check: pid + start time pins one specific process, so a recycled pid can never pass. When the
		// start time is recorded but unreadable we know nothing about identity — that is Unverified, not "yes".
		if (entry.startedAtUtcTicks > 0)
		{
			var started = TryGetStartTicks(process);
			if (started <= 0)
			{
				return Liveness.Unverified;
			}

			return Math.Abs(started - entry.startedAtUtcTicks) <= StartTimeSlack.Ticks
				? Liveness.Running
				: Liveness.Stopped;
		}

		// Legacy run-states (written before the start time existed) have to be identified the hard way.
		string image;
		try
		{
			image = process.ProcessName;
		}
		catch
		{
			return Liveness.Unverified;
		}

		return ClassifyLegacy(image, LocalStackProcess.TryGetCommandLine(entry.pid), identityTokens);
	}

	/// <summary>
	/// The legacy (no recorded start time) decision, as a pure function of what we could observe about the live
	/// pid: its process <paramref name="image"/>, its <paramref name="commandLine"/> (null when unreadable), and
	/// the step's <paramref name="identityTokens"/>.
	///
	/// An image outside <see cref="StackImages"/> is conclusive — that is the audio-service-on-a-recycled-pid
	/// case. A plausible image is NOT conclusive (one live JVM is not another service's), so the step's own
	/// identity has to appear on the command line. When the command line cannot be read, or the step has no
	/// usable token, the honest answer is <see cref="Liveness.Unverified"/> — never a guess in either direction.
	/// </summary>
	public static Liveness ClassifyLegacy(string image, string commandLine, IEnumerable<string> identityTokens)
	{
		if (string.IsNullOrEmpty(image) || !StackImages.Contains(image, StringComparer.OrdinalIgnoreCase))
		{
			return Liveness.Stopped;
		}

		var tokens = identityTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
		if (tokens.Count == 0 || string.IsNullOrEmpty(commandLine))
		{
			return Liveness.Unverified;
		}

		return tokens.Any(t => commandLine.Contains(t, StringComparison.OrdinalIgnoreCase))
			? Liveness.Running
			: Liveness.Stopped;
	}

	/// <summary>
	/// Convenience for callers that want a plain yes/no and are safe to treat <see cref="Liveness.Unverified"/>
	/// as running (i.e. anything but <c>stop</c>, where an unconfirmed pid must not be killed).
	/// </summary>
	public static bool IsEntryRunning(LocalStackRunEntry entry, IEnumerable<string> identityTokens) =>
		Check(entry, identityTokens) != Liveness.Stopped;

	/// <summary>UTC ticks of a pid's start time, or 0 when it can't be read (permissions, already gone).</summary>
	public static long StartTicksOf(int pid)
	{
		if (pid <= 0)
		{
			return 0;
		}

		try
		{
			return TryGetStartTicks(Process.GetProcessById(pid));
		}
		catch
		{
			return 0;
		}
	}

	private static long TryGetStartTicks(Process process)
	{
		try
		{
			return process.StartTime.ToUniversalTime().Ticks;
		}
		catch
		{
			return 0; // some processes deny access to their start time
		}
	}
}

/// <summary>Loads/saves the run-state and resolves its path + the per-run log directory.</summary>
public static class LocalStackRunStateIO
{
	public const string RunStateFileName = "local-stack.run.json";
	public const string LogsDirName = "local-stack-logs";

	private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore,
		DefaultValueHandling = DefaultValueHandling.Include
	};

	/// <summary>The run-state path that sits alongside the given manifest path.</summary>
	public static string ResolveRunStatePath(string manifestPath)
	{
		var dir = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
		return Path.Combine(dir ?? ".", RunStateFileName);
	}

	/// <summary>The workspace log directory base that sits alongside the given manifest path (parent of the
	/// per-run subfolders). Retained for back-compat; prefer <see cref="ResolveRunLogsDir"/>.</summary>
	public static string ResolveLogsDir(string manifestPath)
	{
		var dir = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
		return Path.Combine(dir ?? ".", LogsDirName);
	}

	/// <summary>
	/// Resolves a <b>unique per-run</b> log directory. With <paramref name="save"/> the logs live under the
	/// workspace (<c>&lt;manifestDir&gt;/local-stack-logs/run-&lt;runId&gt;</c>) and are kept; without it they
	/// live under the OS temp dir (<c>&lt;temp&gt;/beam-local-stack/&lt;workspaceHash&gt;/run-&lt;runId&gt;</c>)
	/// and <c>stop</c> deletes them. Every call returns a distinct <c>run-&lt;runId&gt;</c> leaf (timestamp +
	/// pid + random), so concurrent runs, same-second reruns, and separate projects never share a folder or
	/// file — the fixed-path collision that used to crash <c>up</c> when a leftover wrapper held a log.
	/// </summary>
	public static string ResolveRunLogsDir(string manifestPath, bool save)
	{
		var guid8 = Guid.NewGuid().ToString("N").Substring(0, 8);
		var runId = $"run-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}-{guid8}";

		if (save)
		{
			var dir = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? ".";
			return Path.Combine(dir, LogsDirName, runId);
		}

		// Temp: hash the FULL manifest path so two projects that share a folder name don't collide under the
		// shared temp root. The unique run leaf makes actual files collision-proof even if two hashes matched.
		return Path.Combine(ResolveTempLogsBase(manifestPath), runId);
	}

	/// <summary>The temp base holding this workspace's ephemeral per-run log dirs
	/// (<c>&lt;temp&gt;/beam-local-stack/&lt;workspaceHash&gt;</c>). <c>up</c> prunes stale <c>run-*</c>
	/// subfolders here so temp logs from crashed/detached runs don't accumulate.</summary>
	public static string ResolveTempLogsBase(string manifestPath) =>
		Path.Combine(Path.GetTempPath(), "beam-local-stack", WorkspaceHash(manifestPath));

	/// <summary>Short stable hash (12 hex chars) of the full manifest path, for the temp log root segment.</summary>
	private static string WorkspaceHash(string manifestPath)
	{
		var full = Path.GetFullPath(manifestPath);
		// Windows paths are case-insensitive — normalize so the same manifest always hashes the same.
		var normalized = OperatingSystem.IsWindows() ? full.ToLowerInvariant() : full;
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
		return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
	}

	public static LocalStackRunState Load(string path)
	{
		if (!File.Exists(path)) return null;
		var state = JsonConvert.DeserializeObject<LocalStackRunState>(File.ReadAllText(path));
		if (state != null) state.steps ??= new List<LocalStackRunEntry>();
		return state;
	}

	public static void Save(string path, LocalStackRunState state)
	{
		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
		File.WriteAllText(path, JsonConvert.SerializeObject(state, Settings));
	}

	public static void Clear(string path)
	{
		try { if (File.Exists(path)) File.Delete(path); }
		catch { /* best-effort */ }
	}
}
