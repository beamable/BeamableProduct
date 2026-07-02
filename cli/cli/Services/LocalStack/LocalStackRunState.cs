using Newtonsoft.Json;

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

	/// <summary>The launched steps, in start order.</summary>
	public List<LocalStackRunEntry> steps = new List<LocalStackRunEntry>();
}

/// <summary>One launched step in a <see cref="LocalStackRunState"/>.</summary>
public class LocalStackRunEntry
{
	public string name;
	public string group;

	/// <summary>OS process id of the launched service (the exec'd leaf on unix).</summary>
	public int pid;

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

	/// <summary>The per-run log directory that sits alongside the given manifest path.</summary>
	public static string ResolveLogsDir(string manifestPath)
	{
		var dir = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
		return Path.Combine(dir ?? ".", LogsDirName);
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
