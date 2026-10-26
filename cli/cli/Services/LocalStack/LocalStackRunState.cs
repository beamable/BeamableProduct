using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.LocalStack;

/// <summary>
/// The record of a running local stack, written by <c>beam local run</c> and read by
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
	/// one specific process. 0 when the start time couldn't be read at launch (liveness then falls back to checking
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
		"BeamableGateway", "BeamableMessageRailRuntime", "BeamableCampaignRuntime",
		"BeamableSegmentationRuntime", "BeamableAnalyticsLoader"
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

		// No start time recorded (it couldn't be read at launch): the process has to be identified the hard way.
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
		var state = JsonConvert.DeserializeObject<LocalStackRunState>(ReadShared(path));
		if (state != null) state.steps ??= new List<LocalStackRunEntry>();
		return state;
	}

	public static void Save(string path, LocalStackRunState state) =>
		WriteShared(path, JsonConvert.SerializeObject(state, Settings));

	/// <summary>
	/// Writes <paramref name="content"/> to <paramref name="path"/> through a temp file of its own, so a reader never
	/// sees half of it. While another process has the file open (`beam local ps` reading it, a restart run elsewhere
	/// writing it) Windows refuses the write; it is tried again for a moment instead of failing the run, which tore the
	/// whole stack down.
	/// </summary>
	public static void WriteShared(string path, string content)
	{
		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
		var temp = $"{path}.{Guid.NewGuid():N}.tmp";
		File.WriteAllText(temp, content);
		try
		{
			Retry(() => File.Move(temp, path, overwrite: true));
		}
		finally
		{
			if (File.Exists(temp)) File.Delete(temp);
		}
	}

	/// <summary>Reads <paramref name="path"/> without keeping a writer from replacing it, trying again for a moment while
	/// the writer has it open.</summary>
	public static string ReadShared(string path)
	{
		string text = null;
		Retry(() =>
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var reader = new StreamReader(stream);
			text = reader.ReadToEnd();
		});
		return text;
	}

	private static void Retry(Action action)
	{
		for (var attempt = 1; ; attempt++)
		{
			try
			{
				action();
				return;
			}
			catch (IOException e) when (e is not FileNotFoundException and not DirectoryNotFoundException && attempt < 20)
			{
				Thread.Sleep(50);
			}
			catch (UnauthorizedAccessException) when (attempt < 20)
			{
				// A replace over a file another process has open can surface as access denied too.
				Thread.Sleep(50);
			}
		}
	}

	/// <summary>Removes the run-state, and the board <c>beam local ps</c> shows for it.</summary>
	public static void Clear(string path)
	{
		try { if (File.Exists(path)) File.Delete(path); }
		catch { /* best-effort */ }
		LocalStackBoardIO.Clear(LocalStackBoardIO.PathFor(path));
	}
}

/// <summary>What a build step compiled when it last built successfully.</summary>
public class LocalStackBuildRecord
{
	public string fingerprint;
	public DateTime builtUtc;
}

/// <summary>The last successful build of each build step, kept across runs (<c>beam local stop</c> leaves it).</summary>
public class LocalStackBuildState
{
	public Dictionary<string, LocalStackBuildRecord> steps = new Dictionary<string, LocalStackBuildRecord>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Whether a build step needs to build. Without this, a build ran only when its output was missing or <c>:build</c>
/// was passed, so after a pull the stack kept running stale binaries, and <c>:build</c> rebuilt everything. A step's <see cref="LocalStackStep.inputs"/> are fingerprinted from
/// git: the tree id of each input at HEAD, plus a hash of every file git reports as changed or new under it. So a
/// pull, a branch switch, an edit (and a second edit of the same file) all change it, and nothing outside the inputs
/// does, and no file is walked. The fingerprint is saved after each successful build, and the next run builds the
/// step only when it differs. An input outside a git checkout has no fingerprint, and the step then only builds
/// when its output is missing, as before.
/// </summary>
public static class LocalStackBuilds
{
	public const string FileName = "local-stack.builds.json";

	/// <summary>The build-state file next to the settings at <paramref name="manifestPath"/>.</summary>
	public static string PathFor(string manifestPath) =>
		Path.Combine(Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? ".", FileName);

	public static LocalStackBuildState Load(string path)
	{
		try
		{
			var state = File.Exists(path) ? JsonConvert.DeserializeObject<LocalStackBuildState>(File.ReadAllText(path)) : null;
			return new LocalStackBuildState
			{
				steps = new Dictionary<string, LocalStackBuildRecord>(state?.steps ?? new Dictionary<string, LocalStackBuildRecord>(),
					StringComparer.OrdinalIgnoreCase)
			};
		}
		catch
		{
			return new LocalStackBuildState();
		}
	}

	public static void Save(string path, LocalStackBuildState state)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, JsonConvert.SerializeObject(state, Formatting.Indented));
	}

	/// <summary>
	/// Why <paramref name="step"/> should build, or null when it is up to date (or can't tell: no inputs, or not in
	/// git). <paramref name="cache"/> shares the git answers between steps that compile the same folders.
	/// </summary>
	public static string WhyBuild(LocalStackStep step, LocalStackConfig config, LocalStackBuildState state,
		Dictionary<string, string> cache = null)
	{
		var fingerprint = Fingerprint(step, config, out var repo, cache);
		if (fingerprint == null) return null;
		if (!state.steps.TryGetValue(step.name, out var record) || string.IsNullOrEmpty(record.fingerprint))
			return "not built by beam local run yet";
		return record.fingerprint == fingerprint ? null : $"{repo} changed since the last build";
	}

	/// <summary>True when the step's inputs have a fingerprint and it is the one saved after its last build.</summary>
	public static bool IsUpToDate(LocalStackStep step, LocalStackConfig config, LocalStackBuildState state, Dictionary<string, string> cache = null)
	{
		var fingerprint = Fingerprint(step, config, out _, cache);
		return fingerprint != null && state.steps.TryGetValue(step.name, out var record) && record.fingerprint == fingerprint;
	}

	/// <summary>Saves what the step compiled, after it built successfully. Read fresh: the build may have changed tracked files.</summary>
	public static void Record(LocalStackStep step, LocalStackConfig config, LocalStackBuildState state, string path)
	{
		var fingerprint = Fingerprint(step, config, out _);
		if (fingerprint == null) return;
		state.steps[step.name] = new LocalStackBuildRecord { fingerprint = fingerprint, builtUtc = DateTime.UtcNow };
		Save(path, state);
	}

	/// <summary>The step's inputs as one hash, or null without inputs or outside git. <paramref name="repo"/> is the
	/// first input's repo folder name, for the reason shown.</summary>
	public static string Fingerprint(LocalStackStep step, LocalStackConfig config, out string repo,
		Dictionary<string, string> cache = null)
	{
		repo = null;
		var inputs = step.inputs;
		if (inputs == null || inputs.Count == 0) return null;

		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		var parts = new List<string>();
		foreach (var input in inputs)
		{
			var full = Path.GetFullPath(Path.Combine(workDir ?? Directory.GetCurrentDirectory(), LocalStackConfigIO.Substitute(input, config)));
			var probe = Directory.Exists(full) ? full : Path.GetDirectoryName(full);
			if (string.IsNullOrEmpty(probe) || !Directory.Exists(probe)) return null;

			var top = Git(probe, "rev-parse", "--show-toplevel")?.Trim();
			if (string.IsNullOrEmpty(top)) return null;
			top = Path.GetFullPath(top);
			repo ??= Path.GetFileName(top.TrimEnd('/', '\\'));

			var rel = Path.GetRelativePath(top, full).Replace('\\', '/');
			var key = top + "|" + rel;
			if (cache == null || !cache.TryGetValue(key, out var part))
			{
				part = InputPart(top, rel);
				if (cache != null) cache[key] = part;
			}

			parts.Add(part);
		}

		return Sha(string.Join("\n", parts));
	}

	/// <summary>A folder's sources as one hash, the way a build input is fingerprinted; null outside git.</summary>
	public static string FolderFingerprint(string folder)
	{
		if (!Directory.Exists(folder)) return null;
		var top = Git(folder, "rev-parse", "--show-toplevel")?.Trim();
		if (string.IsNullOrEmpty(top)) return null;
		top = Path.GetFullPath(top);
		return Sha(InputPart(top, Path.GetRelativePath(top, Path.GetFullPath(folder)).Replace('\\', '/')));
	}

	/// <summary>One input: its tree (or blob) id at HEAD, and a hash of each changed or new file under it.</summary>
	private static string InputPart(string top, string rel)
	{
		var tree = Git(top, "rev-parse", rel == "." ? "HEAD^{tree}" : $"HEAD:{rel}")?.Trim() ?? "not in HEAD";
		var lines = new List<string> { $"{rel}={tree}" };
		foreach (var file in ChangedFiles(Git(top, "status", "--porcelain=v1", "-uall", "-z", "--", rel) ?? string.Empty))
		{
			var path = Path.Combine(top, file);
			lines.Add($"{file}:{(File.Exists(path) ? Sha(File.ReadAllBytes(path)) : "deleted")}");
		}

		return string.Join("\n", lines);
	}

	/// <summary>The paths in <c>git status --porcelain=v1 -z</c> output, sorted. A rename's second entry is its old
	/// path, which is skipped.</summary>
	public static List<string> ChangedFiles(string status)
	{
		var files = new List<string>();
		var entries = status.Split('\0');
		for (var i = 0; i < entries.Length; i++)
		{
			var entry = entries[i];
			if (entry.Length < 4) continue;
			files.Add(entry.Substring(3));
			if (entry[0] is 'R' or 'C') i++;
		}

		files.Sort(StringComparer.Ordinal);
		return files;
	}

	private static string Sha(string text) => Sha(System.Text.Encoding.UTF8.GetBytes(text));

	private static string Sha(byte[] bytes) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));

	/// <summary>Runs git in <paramref name="dir"/>; its output, or null when git is missing or fails.</summary>
	private static string Git(string dir, params string[] args)
	{
		try
		{
			var psi = new System.Diagnostics.ProcessStartInfo("git")
			{
				WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true,
				UseShellExecute = false, CreateNoWindow = true
			};
			foreach (var arg in args) psi.ArgumentList.Add(arg);
			using var process = System.Diagnostics.Process.Start(psi);
			if (process == null) return null;
			var errors = process.StandardError.ReadToEndAsync();
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit(30000);
			_ = errors.Result;
			return process.HasExited && process.ExitCode == 0 ? output : null;
		}
		catch
		{
			return null;
		}
	}
}

/// <summary>What another terminal asks a running stack to do (restart, start or stop steps, or change stack).</summary>
public class LocalStackControlRequest
{
	public string id;

	/// <summary><c>restart</c>, <c>start</c>, <c>stop</c>, or <c>adjust</c> (run another stack name in place).</summary>
	public string action;

	/// <summary>The steps it is about, by name, label or tag; empty for <c>stop</c> means the whole stack.</summary>
	public List<string> items = new List<string>();

	/// <summary>For <c>adjust</c>: the stack name to change to, as <c>beam local run</c> takes it.</summary>
	public string stack;
}

/// <summary>The running stack's answer.</summary>
public class LocalStackControlResponse
{
	public string id;
	public bool ok;
	public string message;

	/// <summary>The steps the request acted on.</summary>
	public List<string> steps = new List<string>();
}

/// <summary>
/// How another terminal talks to an attached <c>beam local run</c>, whose services live inside its own process
/// (they stop with it). A request is a file in <c>local-stack.control/</c> next to the run-state; the run picks it
/// up within a second, does the work, and writes the answer next to it.
/// </summary>
public static class LocalStackControl
{
	public const string DirName = "local-stack.control";

	public static string DirFor(string runStatePath) =>
		Path.Combine(Path.GetDirectoryName(runStatePath) ?? ".", DirName);

	/// <summary>Sends <paramref name="request"/> and waits for the answer; null when none came in time.</summary>
	public static async Task<LocalStackControlResponse> Ask(string runStatePath, LocalStackControlRequest request, TimeSpan timeout,
		CancellationToken token)
	{
		var dir = DirFor(runStatePath);
		Directory.CreateDirectory(dir);
		request.id ??= Guid.NewGuid().ToString("N");
		var requestFile = Path.Combine(dir, request.id + ".request.json");
		var responseFile = Path.Combine(dir, request.id + ".response.json");
		File.WriteAllText(requestFile + ".tmp", JsonConvert.SerializeObject(request));
		File.Move(requestFile + ".tmp", requestFile, overwrite: true);

		var deadline = DateTime.UtcNow + timeout;
		try
		{
			while (DateTime.UtcNow < deadline)
			{
				token.ThrowIfCancellationRequested();
				if (File.Exists(responseFile))
				{
					try
					{
						return JsonConvert.DeserializeObject<LocalStackControlResponse>(File.ReadAllText(responseFile));
					}
					catch (IOException)
					{
						// Still being written.
					}
				}

				await Task.Delay(250, token);
			}

			return null;
		}
		finally
		{
			TryDelete(requestFile);
			TryDelete(responseFile);
		}
	}

	/// <summary>The requests waiting for the run, oldest first. Each is taken: its file is removed.</summary>
	public static List<LocalStackControlRequest> Take(string runStatePath)
	{
		var dir = DirFor(runStatePath);
		var requests = new List<LocalStackControlRequest>();
		if (!Directory.Exists(dir)) return requests;

		foreach (var file in new DirectoryInfo(dir).GetFiles("*.request.json").OrderBy(f => f.LastWriteTimeUtc))
		{
			try
			{
				var request = JsonConvert.DeserializeObject<LocalStackControlRequest>(File.ReadAllText(file.FullName));
				file.Delete();
				if (request?.id != null) requests.Add(request);
			}
			catch (IOException)
			{
				// Being written; the next poll takes it.
			}
			catch (JsonException)
			{
				TryDelete(file.FullName);
			}
		}

		return requests;
	}

	public static void Answer(string runStatePath, LocalStackControlResponse response)
	{
		var file = Path.Combine(DirFor(runStatePath), response.id + ".response.json");
		File.WriteAllText(file + ".tmp", JsonConvert.SerializeObject(response));
		File.Move(file + ".tmp", file, overwrite: true);
	}

	/// <summary>
	/// The steps of <paramref name="steps"/> that <paramref name="items"/> name: by step name, by label, or by tag.
	/// Build steps are left out of tag matches (a restart runs the build check itself). Stack order is kept. Throws
	/// with what can be named when an item matches nothing.
	/// </summary>
	public static List<LocalStackStep> Match(IEnumerable<string> items, IReadOnlyList<LocalStackStep> steps, Func<LocalStackStep, IEnumerable<string>> tagsOf)
	{
		var matched = new HashSet<LocalStackStep>();
		foreach (var raw in items.Select(i => i?.Trim()).Where(i => !string.IsNullOrEmpty(i)))
		{
			var byName = steps.Where(s => string.Equals(s.name, raw, StringComparison.OrdinalIgnoreCase)
			                              || string.Equals(LabelOf(s), raw, StringComparison.OrdinalIgnoreCase)).ToList();
			var hits = byName.Count > 0
				? byName
				: steps.Where(s => !s.build && tagsOf(s).Contains(raw, StringComparer.OrdinalIgnoreCase)).ToList();
			if (hits.Count == 0)
				throw new CliException($"'{raw}' isn't a step, label or tag of the running stack. Its steps: {string.Join(", ", steps.Where(s => !s.build).Select(s => s.name))}.");
			matched.UnionWith(hits);
		}

		return steps.Where(matched.Contains).ToList();
	}

	/// <summary>
	/// The build steps that build what <paramref name="step"/> runs: the one named after it (<c>build: c# gateway</c>
	/// for <c>c# gateway</c>), otherwise the ones of its kind before it (<c>build: scala</c> for a Scala service,
	/// <c>build: portal deps</c> for the portal, <c>build: microservices</c> for a microservice).
	/// </summary>
	public static List<LocalStackStep> BuildsFor(LocalStackStep step, IReadOnlyList<LocalStackStep> steps, Func<LocalStackStep, IEnumerable<string>> tagsOf)
	{
		if (step.build) return new List<LocalStackStep>();
		var named = steps.Where(s => s.build && string.Equals(s.name, "build: " + step.name, StringComparison.OrdinalIgnoreCase)).ToList();
		if (named.Count > 0) return named;

		var kinds = tagsOf(step).Where(t => t is "scala" or "portal" or LocalStackStacks.MicroservicesTag).ToList();
		var index = steps.ToList().IndexOf(step);
		return steps.Take(index < 0 ? steps.Count : index)
			.Where(s => s.build && tagsOf(s).Any(t => kinds.Contains(t, StringComparer.OrdinalIgnoreCase))
			            && !s.name.StartsWith("build: c# ", StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	private static string LabelOf(LocalStackStep step) =>
		string.IsNullOrWhiteSpace(step.label) ? LocalStackConfigIO.InferLabel(step.name ?? string.Empty) : step.label;

	private static void TryDelete(string file)
	{
		try
		{
			if (File.Exists(file)) File.Delete(file);
		}
		catch
		{
			// best-effort
		}
	}
}
