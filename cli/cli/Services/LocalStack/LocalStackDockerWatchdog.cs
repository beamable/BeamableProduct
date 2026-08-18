using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace cli.Services.LocalStack;

/// <summary>
/// Watches the containers a manifest's <c>docker compose up</c> steps brought up, for as long as
/// <c>beam local up</c> stays attached.
///
/// The attached wait (<c>LocalStackUpCommand.WaitAttached</c>) only races the <em>host process</em> exit tasks.
/// Docker steps are <see cref="LocalStackStep.waitForExit"/> run-to-completion steps — the compose CLI exits
/// as soon as the containers are started — so their containers are invisible to it and can die at any point
/// afterwards without the orchestrator forming an opinion. That happened on 2026-08-07: <c>mongo_master</c>
/// SIGSEGV'd (exit 139) 29 minutes after a clean bring-up, then Docker Desktop restarted its VM and took every
/// remaining container with it (exit 255). Nothing restarted them (every service in both compose files is
/// <c>RestartPolicy: no</c>), and <c>up</c> — which had already reported "Stack is up" — streamed reconnect
/// spam for another 27 minutes into a 4-million-line log without ever noticing.
///
/// This turns that silence into a loud, correctly-attributed event and re-runs the owning compose step.
/// </summary>
public static class LocalStackDockerWatchdog
{
	/// <summary>How long to give a read-only <c>docker</c> query before killing it (matches EnsureDockerRunning).</summary>
	private const int QueryTimeoutMs = 15000;

	/// <summary>
	/// How long to give a restart. Much longer than a query: the api-deps step is <c>compose up -d --wait</c>,
	/// which blocks until every container passes its healthcheck (mongo's replica-set init alone outlasts a
	/// query timeout), so a 15s budget here would report a restart that is working fine as a failure.
	/// </summary>
	private const int RestartTimeoutMs = 300000;

	/// <summary>Restart attempts per step when it doesn't set <see cref="LocalStackStep.readyRetries"/>.</summary>
	public const int DefaultRestartAttempts = 2;

	/// <summary>One compose project the watchdog polls, derived from a <c>docker compose up</c> step.</summary>
	public class Target
	{
		/// <summary>The step that brought these containers up; also supplies the restart command.</summary>
		public LocalStackStep step;

		/// <summary>Directory the compose command runs in — this is what selects the compose project.</summary>
		public string workingDirectory;

		/// <summary>
		/// The compose services the step explicitly asked for. Empty means the step brought up the whole
		/// project, so every service in it is watched.
		/// </summary>
		public List<string> services = new List<string>();

		/// <summary>
		/// This project's own liveness baseline. Per target, because each compose project is polled separately
		/// and so seals its baseline from its own first poll.
		/// </summary>
		public LivenessTracker liveness = new LivenessTracker();

		/// <summary>True when this target watches <paramref name="service"/>.</summary>
		public bool Watches(string service) =>
			services.Count == 0 ||
			services.Contains(service, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>One row of <c>docker compose ps --all --format json</c>.</summary>
	public class ContainerState
	{
		[JsonProperty("Service")] public string service;
		[JsonProperty("Name")] public string name;
		[JsonProperty("State")] public string state;
		[JsonProperty("ExitCode")] public int exitCode;

		public bool IsRunning => string.Equals(state, "running", StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Dead on this container's own evidence: not running, and it exited non-zero. Deliberately does NOT
		/// judge a clean exit, which needs the run's history to interpret — see <see cref="LivenessTracker"/>.
		/// </summary>
		public bool IsDead => !IsRunning && exitCode != 0;

		/// <summary>Why this exit code happened, for the exit codes this stack actually produces.</summary>
		public string Explain() => exitCode switch
		{
			139 => "SIGSEGV — the container's process crashed (mongo_master is known to segfault inside " +
			       "WiredTiger on a ServiceDiscovery TTL write; the fatal stack is in its docker logs)",
			137 => "SIGKILL — killed by the daemon or out of memory",
			255 => "died with the Docker VM — Docker Desktop restarted or shut down its VM, which takes every " +
			       "container with it because none declare a restart policy",
			0 => "exited cleanly, but it was running earlier in this session — something stopped it",
			_ => "exited unexpectedly",
		};
	}

	/// <summary>
	/// Decides which containers in a poll are dead, judged against the steady state that existed when the stack
	/// was declared up.
	///
	/// <see cref="ContainerState.IsDead"/> alone keys off a non-zero exit code. That catches a SIGSEGV (139), a
	/// Docker-VM teardown (255) and a SIGKILL (137) — but <b>not</b> a clean exit, and a clean exit is a real
	/// failure mode: <c>docker stop mongo_master</c> yields <c>exited 0</c> because mongod handles SIGTERM, and a
	/// vanished mongo_master is fatal to the stack however politely it left.
	///
	/// A single sample can't tell that apart from the one-shot <c>mongo_*_setup</c> init containers, which sit at
	/// <c>exited 0</c> for the whole run by design. The <b>first poll</b> separates them: whatever is running once
	/// the stack is up is the baseline this watchdog maintains, and a baseline container that later stops is dead
	/// regardless of exit code. Containers that appear afterwards are deliberately NOT adopted into the baseline —
	/// restarting a step re-runs its init containers, and their normal completion must not read as a failure.
	/// </summary>
	public class LivenessTracker
	{
		private readonly HashSet<string> _baseline = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private bool _baselineSealed;

		/// <summary>True when this container was part of the steady state at the first poll.</summary>
		public bool InBaseline(string containerName) =>
			!string.IsNullOrEmpty(containerName) && _baseline.Contains(containerName);

		/// <summary>
		/// Seals the baseline from the first poll it is given, then returns the dead containers in this poll.
		/// Call once per poll, with that poll's full container list.
		/// </summary>
		public List<ContainerState> Dead(List<ContainerState> poll)
		{
			var containers = poll ?? new List<ContainerState>();

			if (!_baselineSealed)
			{
				foreach (var container in containers.Where(c => c != null && c.IsRunning))
					if (!string.IsNullOrEmpty(container.name))
						_baseline.Add(container.name);

				_baselineSealed = true;
			}

			return containers.Where(c => c != null && !c.IsRunning && (c.IsDead || InBaseline(c.name))).ToList();
		}
	}

	/// <summary>
	/// The compose projects worth polling: every enabled step that shells out to <c>docker</c> to run a
	/// <c>compose up</c>. A <c>compose down</c>/<c>stop</c> step, or any non-docker step, owns no running
	/// containers and yields no target.
	/// </summary>
	public static List<Target> DiscoverTargets(IEnumerable<LocalStackStep> steps, LocalStackConfig config)
	{
		var targets = new List<Target>();
		if (steps == null) return targets;

		foreach (var step in steps)
		{
			if (step == null || !step.enabled || step.build) continue;
			if (!string.Equals(step.command, "docker", StringComparison.OrdinalIgnoreCase)) continue;

			var tokens = Tokenize(step.arguments);
			var up = IndexOfSubcommand(tokens, "compose", "up");
			if (up < 0) continue;

			targets.Add(new Target
			{
				step = step,
				workingDirectory = LocalStackConfigIO.Substitute(step.workingDirectory, config),
				services = ServiceNamesAfterUp(tokens, up),
			});
		}

		return targets;
	}

	/// <summary>
	/// The positional service names a <c>compose up</c> names, e.g. <c>compose up -d --no-deps redis</c> →
	/// <c>["redis"]</c>. Flags are skipped, along with the value of any flag that takes one (<c>--profile web</c>,
	/// <c>-f other.yml</c>) so the value is never mistaken for a service. An empty result means "the whole
	/// project", which is what <c>compose up -d --wait</c> does.
	/// </summary>
	public static List<string> ServiceNamesAfterUp(List<string> tokens, int upIndex)
	{
		// Flags that consume the next token. Anything else starting with '-' is a bare switch (-d, --wait, --no-deps).
		var takesValue = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"-f", "--file", "-p", "--project-name", "--profile", "--project-directory",
			"--scale", "--timeout", "-t", "--exit-code-from", "--pull", "--attach", "--no-attach",
		};

		var services = new List<string>();
		for (var i = upIndex + 1; i < tokens.Count; i++)
		{
			var token = tokens[i];
			if (string.IsNullOrWhiteSpace(token)) continue;

			if (token.StartsWith("-", StringComparison.Ordinal))
			{
				// "--profile=web" carries its value inline, so it consumes nothing extra.
				if (takesValue.Contains(token) && !token.Contains('='))
					i++;
				continue;
			}

			services.Add(token);
		}

		return services;
	}

	/// <summary>
	/// Polls one target and returns every container it watches. Returns an empty list when docker can't be
	/// reached or the output can't be parsed — the watchdog must never turn a transient docker hiccup into a
	/// reported service death.
	/// </summary>
	public static List<ContainerState> Poll(Target target, string dockerPath)
	{
		var (ok, stdout, _) = RunDocker(dockerPath, target.workingDirectory, QueryTimeoutMs,
			"compose", "ps", "--all", "--format", "json");
		if (!ok) return new List<ContainerState>();

		return ParsePs(stdout).Where(c => target.Watches(c.service)).ToList();
	}

	/// <summary>
	/// Parses <c>docker compose ps --format json</c>. Compose v2 emits one JSON object per line; some versions
	/// emit a single JSON array instead. Both are accepted, and an unparseable line is skipped rather than
	/// failing the whole poll.
	/// </summary>
	public static List<ContainerState> ParsePs(string stdout)
	{
		var results = new List<ContainerState>();
		if (string.IsNullOrWhiteSpace(stdout)) return results;

		var text = stdout.Trim();
		if (text.StartsWith("[", StringComparison.Ordinal))
		{
			try
			{
				var array = JsonConvert.DeserializeObject<List<ContainerState>>(text);
				if (array != null) results.AddRange(array.Where(c => c != null));
			}
			catch { /* fall through to line-by-line — a truncated array is better read as lines than dropped */ }

			if (results.Count > 0) return results;
		}

		foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			var trimmed = line.Trim().TrimEnd(',');
			if (trimmed.Length == 0 || trimmed[0] != '{') continue;

			try
			{
				var parsed = JsonConvert.DeserializeObject<ContainerState>(trimmed);
				if (parsed != null && !string.IsNullOrWhiteSpace(parsed.name)) results.Add(parsed);
			}
			catch { /* skip the malformed row, keep the rest of the poll */ }
		}

		return results;
	}

	/// <summary>The tail of a container's log, for reporting a death with its cause attached.</summary>
	public static string LogTail(string dockerPath, string containerName, int lines)
	{
		var (ok, stdout, stderr) = RunDocker(dockerPath, null, QueryTimeoutMs,
			"logs", containerName, "--tail", lines.ToString());
		if (!ok) return null;

		// A crashing container's fatal message usually goes to stderr, so both streams matter here.
		var text = string.Join("\n", new[] { stdout, stderr }.Where(s => !string.IsNullOrWhiteSpace(s)));
		return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
	}

	/// <summary>
	/// Re-runs a target's own <c>docker compose up</c> to bring its containers back. Returns true when the
	/// compose command succeeded.
	/// </summary>
	public static bool Restart(Target target, string dockerPath, LocalStackConfig config, out string error)
	{
		var arguments = LocalStackConfigIO.Substitute(target.step.arguments, config);
		var (ok, _, stderr) = RunDocker(dockerPath, target.workingDirectory, RestartTimeoutMs,
			Tokenize(arguments).ToArray());
		error = ok ? null : Summarize(stderr);
		return ok;
	}

	/// <summary>How many restarts this step is allowed per container.</summary>
	public static int RestartAttemptsFor(LocalStackStep step) =>
		step != null && step.readyRetries > 0 ? step.readyRetries : DefaultRestartAttempts;

	/// <summary>
	/// Splits a command line on whitespace, honouring double quotes so a quoted path survives as one token.
	/// </summary>
	public static List<string> Tokenize(string commandLine)
	{
		var tokens = new List<string>();
		if (string.IsNullOrWhiteSpace(commandLine)) return tokens;

		var current = new StringBuilder();
		var inQuotes = false;

		foreach (var c in commandLine)
		{
			if (c == '"')
			{
				inQuotes = !inQuotes;
				continue;
			}

			if (!inQuotes && char.IsWhiteSpace(c))
			{
				if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
				continue;
			}

			current.Append(c);
		}

		if (current.Length > 0) tokens.Add(current.ToString());
		return tokens;
	}

	/// <summary>
	/// The index of <paramref name="second"/> in a <c>&lt;first&gt; &lt;second&gt;</c> subcommand pair, ignoring
	/// any global flags between them (<c>compose -f x.yml up</c>), or -1 when the pair isn't there.
	/// </summary>
	public static int IndexOfSubcommand(List<string> tokens, string first, string second)
	{
		if (tokens == null) return -1;

		var firstAt = tokens.FindIndex(t => string.Equals(t, first, StringComparison.OrdinalIgnoreCase));
		if (firstAt < 0) return -1;

		var secondAt = tokens.FindIndex(firstAt + 1,
			t => string.Equals(t, second, StringComparison.OrdinalIgnoreCase));
		return secondAt;
	}

	/// <summary>
	/// Runs docker and captures its output. Never throws — every caller treats failure as "no information",
	/// because a watchdog that can crash the run it is monitoring is worse than no watchdog.
	/// </summary>
	private static (bool ok, string stdout, string stderr) RunDocker(
		string dockerPath, string workingDirectory, int timeoutMs, params string[] arguments)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = dockerPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};

			if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
				psi.WorkingDirectory = workingDirectory;

			foreach (var argument in arguments)
				psi.ArgumentList.Add(argument);

			using var proc = Process.Start(psi);
			if (proc == null) return (false, null, "could not start docker");

			// Read both streams before waiting — a full pipe buffer would otherwise deadlock the wait.
			var stdoutTask = proc.StandardOutput.ReadToEndAsync();
			var stderrTask = proc.StandardError.ReadToEndAsync();

			if (!proc.WaitForExit(timeoutMs))
			{
				try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
				return (false, null, $"docker did not respond within {timeoutMs / 1000}s");
			}

			return (proc.ExitCode == 0, stdoutTask.Result, stderrTask.Result);
		}
		catch (Exception e)
		{
			return (false, null, e.Message);
		}
	}

	/// <summary>First non-empty line of a docker error, so a report stays one line.</summary>
	private static string Summarize(string text) =>
		string.IsNullOrWhiteSpace(text)
			? "docker reported a non-zero exit"
			: text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? text.Trim();
}
