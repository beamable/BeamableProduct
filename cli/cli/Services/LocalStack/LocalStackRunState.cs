using Newtonsoft.Json;
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
	/// reverse a run-to-completion step (e.g. <c>compose down</c>).</summary>
	public string stopArguments;

	/// <summary>True for run-to-completion steps (e.g. <c>docker compose up -d</c>): their <see cref="pid"/>
	/// is expected to be dead; liveness is not judged by it.</summary>
	public bool waitForExit;
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
