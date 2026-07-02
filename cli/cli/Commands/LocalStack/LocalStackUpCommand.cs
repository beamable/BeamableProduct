using Beamable.Server;
using cli.Services.LocalStack;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace cli.Commands.LocalStack;

public class LocalStackUpCommandArgs : CommandArgs
{
	public string configPath;
	public string host;
	public string portalUrl;
	public string only;
	public string skip;
	public bool detach;
	public bool createRealm;
	public string realmCustomer;
	public string realmProject;
	public string realmEmail;
	public string realmAlias;
	public string realmPassword;
}

public class LocalStackUpResultStream
{
	/// <summary>The step this update is about (empty for stack-level messages).</summary>
	public string step;

	/// <summary>One of: starting, ready, running, failed, skipped, stopped, tearing-down.</summary>
	public string status;

	/// <summary>Human-readable detail.</summary>
	public string message;

	/// <summary>0..1 across the whole bring-up.</summary>
	public float progressRatio;
}

/// <summary>
/// Brings up every enabled step in the manifest in order, waiting for each readiness gate and streaming
/// progress. Long-running services are launched <b>detached</b> (their stdout/stderr redirected to per-step
/// log files) and recorded in a run-state file, so they survive this command returning — like
/// <c>docker compose up -d</c>. Use <c>beam local ps</c> / <c>logs</c> / <c>stop</c> to manage them, or pass
/// <c>--attach</c> to tail the logs in the foreground (Ctrl+C detaches; the stack keeps running).
/// </summary>
public class LocalStackUpCommand
	: StreamCommand<LocalStackUpCommandArgs, LocalStackUpResultStream>
	, IStandaloneCommand, ISkipManifest
{
	private readonly object _launchedLock = new object();

	public LocalStackUpCommand() : base("up", "Bring up the local stack from the manifest and tail it (use --detach to return immediately)")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--config", "Path to the manifest (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<string>("--host", "Override the manifest backend host"),
			(args, v) => args.host = v);
		AddOption(new Option<string>("--portal-url", "Override the manifest portal URL"),
			(args, v) => args.portalUrl = v);
		AddOption(new Option<string>("--only", "Run only these steps (comma/space separated names)"),
			(args, v) => args.only = v);
		AddOption(new Option<string>("--skip", "Skip these steps (comma/space separated names)"),
			(args, v) => args.skip = v);
		var detach = new Option<bool>("--detach", "Return immediately after bring-up instead of tailing (the stack keeps running; manage it with ps/logs/stop)");
		detach.AddAlias("-d");
		AddOption(detach, (args, v) => args.detach = v);

		AddOption(new Option<bool>("--create-realm", "Create a fresh local customer/realm via the backend and write it to the workspace config (use after a docker cleanup)"),
			(args, v) => args.createRealm = v);
		AddOption(new Option<string>("--realm-customer", () => "beam", "Customer name to use when creating the local realm"),
			(args, v) => args.realmCustomer = v);
		AddOption(new Option<string>("--realm-project", () => "beam-project", "Project name to use when creating the local realm"),
			(args, v) => args.realmProject = v);
		AddOption(new Option<string>("--realm-email", () => "beam@beamable.com", "Account email to use when creating the local realm"),
			(args, v) => args.realmEmail = v);
		AddOption(new Option<string>("--realm-alias", () => "beam-project", "Alias to use when creating the local realm"),
			(args, v) => args.realmAlias = v);
		AddOption(new Option<string>("--realm-password", () => "123456", "Account password to use when creating the local realm"),
			(args, v) => args.realmPassword = v);
	}

	private static HashSet<string> NameSet(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			// Split on comma only — step names contain spaces (e.g. "portal frontend", "scala: gateway").
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

	public override async Task Handle(LocalStackUpCommandArgs args)
	{
		var path = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		if (!File.Exists(path))
			throw new CliException($"No local-stack manifest at {path}. Run `beam local init` to create one.");

		var config = LocalStackConfigIO.Load(path);
		if (!string.IsNullOrWhiteSpace(args.host)) config.host = args.host;
		if (!string.IsNullOrWhiteSpace(args.portalUrl)) config.portalUrl = args.portalUrl;
		config.javaHome = ResolveJavaHome(args, config);

		var only = NameSet(args.only);
		var skip = NameSet(args.skip);

		bool Included(LocalStackStep s) =>
			s.enabled
			&& (only == null || only.Contains(s.name))
			&& (skip == null || !skip.Contains(s.name));

		var steps = config.steps.Where(Included).ToList();
		if (steps.Count == 0)
			throw new CliException("No steps to run (all disabled or filtered out).");

		// Resolve how to invoke this same beam CLI for `beam: true` steps, and the workspace to run them from.
		var (beamExe, beamLeading) = ResolveBeam(args);
		var beamWorkspaceFallback = args.ConfigService?.BeamableWorkspace
		                            ?? args.ConfigService?.WorkingDirectory
		                            ?? Directory.GetCurrentDirectory();

		var runStatePath = LocalStackRunStateIO.ResolveRunStatePath(path);
		var logsDir = LocalStackRunStateIO.ResolveLogsDir(path);
		Directory.CreateDirectory(logsDir);

		// Idempotency: carry over any steps from a previous `up` that are still running (alive pid), and skip
		// re-launching them below. This is what lets you re-run `up` (e.g. to add the portal) without restarting
		// the Scala backend — and avoids duplicate processes fighting over the same ports.
		var existing = LocalStackRunStateIO.Load(runStatePath);
		var aliveByName = new Dictionary<string, LocalStackRunEntry>(StringComparer.OrdinalIgnoreCase);
		if (existing?.steps != null)
			foreach (var e in existing.steps)
				if (!e.waitForExit && IsPidAlive(e.pid))
					aliveByName[e.name] = e;
		var runState = new LocalStackRunState
		{
			host = config.host, portalUrl = config.portalUrl,
			steps = aliveByName.Values.ToList()
		};
		LocalStackRunStateIO.Save(runStatePath, runState);

		var launched = new List<Launched>();
		var token = args.Lifecycle.CancellationToken;
		var realmEnsured = false;

		// Launch a step and record it in the run-state (upsert by name, so a retry replaces the dead entry
		// rather than duplicating it). Returned as a local function so readiness can relaunch on early exit.
		Launched LaunchAndRegister(LocalStackStep step)
		{
			var l = StartStep(step, config, beamExe, beamLeading, beamWorkspaceFallback, logsDir);
			lock (_launchedLock)
			{
				launched.Add(l);
				var entry = runState.steps.FirstOrDefault(e => e.name == step.name);
				if (entry == null)
				{
					entry = new LocalStackRunEntry { name = step.name };
					runState.steps.Add(entry);
				}

				entry.group = step.group;
				entry.pid = SafePid(l);
				entry.kind = l.Kind;
				entry.stdoutLog = l.StdoutLog;
				entry.stderrLog = l.StderrLog;
				entry.workingDirectory = l.WorkingDirectory;
				entry.command = step.command;
				entry.stopArguments = LocalStackConfigIO.Substitute(step.stopArguments, config);
				entry.waitForExit = step.waitForExit;
				LocalStackRunStateIO.Save(runStatePath, runState);
			}

			Log.Information($"[{step.name}] started (pid={SafePid(l)}), logs: {l.StdoutLog}");
			return l;
		}

		try
		{
			var i = 0;
			while (i < steps.Count)
			{
				token.ThrowIfCancellationRequested();

				// Gather the next batch: a run of consecutive steps sharing a non-empty group is launched and
				// awaited in parallel; an ungrouped step is a batch of one (sequential).
				var groupName = steps[i].group;
				var batch = new List<int> { i };
				if (!string.IsNullOrEmpty(groupName))
				{
					while (i + 1 < steps.Count && steps[i + 1].group == groupName)
						batch.Add(++i);
				}
				i++;

				if (batch.Count > 1)
					Log.Information($"Starting {batch.Count} '{groupName}' steps in parallel.");

				var awaits = new List<Task>();
				foreach (var idx in batch)
				{
					token.ThrowIfCancellationRequested();
					var step = steps[idx];
					var baseProgress = (float)idx / steps.Count;

					// Already running from a previous `up`? Leave it alone.
					if (aliveByName.TryGetValue(step.name, out var running))
					{
						Send(step.name, "running", $"already running (pid={running.pid}) — skipping", baseProgress + 1f / steps.Count);
						Log.Information($"[{step.name}] already running (pid={running.pid}) — skipping.");
						continue;
					}

					// Already serving on its HTTP readiness endpoint (e.g. a gateway/portal from a previous
					// session, the IDE, or a stray process)? Don't launch a conflicting duplicate that would
					// fail to bind the port and hang at "still starting".
					if (await AlreadyServing(step, config, token))
					{
						Send(step.name, "running", "already serving — skipping launch", baseProgress + 1f / steps.Count);
						Log.Information($"[{step.name}] already serving at its readiness endpoint — skipping launch.");
						continue;
					}

					// Ensure a valid local realm/login before the first beam step — microservices and
					// extensions authenticate against the local backend on startup.
					if (step.beam && !realmEnsured)
					{
						realmEnsured = true;
						await EnsureRealmAndLogin(args, config);
					}

					Send(step.name, "starting", $"launching ({idx + 1}/{steps.Count})", baseProgress);

					var stepToRun = step;
					var l = LaunchAndRegister(stepToRun);
					awaits.Add(AwaitStep(stepToRun, l, config, baseProgress, steps.Count, token,
						() => LaunchAndRegister(stepToRun)));
				}

				await Task.WhenAll(awaits);
			}

			// No beam steps ran the hook above — still ensure/validate the realm once the backend is up.
			if (!realmEnsured)
				await EnsureRealmAndLogin(args, config);

			Send(string.Empty, "running",
				$"Stack is up. Backend={config.host} Portal={config.portalUrl}.", 1f);
			Log.Information($"Stack is up. Backend={config.host}  Portal={config.portalUrl}");

			if (args.detach)
			{
				// Fire-and-return (docker compose up -d style). NOTE: don't use this from an IDE run-config —
				// when this process exits the IDE ends the run session and kills the child process tree.
				Log.Information("Detached — the stack keeps running. Manage it with: beam local ps | beam local logs | beam local stop");
			}
			else
			{
				// Default: stay in the foreground and tail. This keeps this process alive so the children stay
				// running (and, under an IDE run-config, the run session stays alive instead of reaping them).
				Log.Information("Tailing child logs — press Ctrl+C to stop tailing. The stack keeps running; use `beam local stop` to bring it down.");
				await TailToConsole(runState, token);
			}
		}
		catch (OperationCanceledException)
		{
			// Cancellation (Ctrl+C while attached, or during bring-up): leave the stack running, per the
			// detached model. The run-state remains so `beam local stop` can bring it down.
			Send(string.Empty, "running", "detached — stack left running (use `beam local stop`)", 1f);
			Log.Information("Detached — stack left running. Use `beam local stop` to bring it down.");
		}
		catch (Exception)
		{
			// A genuine bring-up failure: tear down what we started to avoid orphans, and clear the run-state.
			TearDown(launched);
			LocalStackRunStateIO.Clear(runStatePath);
			throw;
		}
	}

	private void Send(string step, string status, string message, float progress) =>
		SendResults(new LocalStackUpResultStream
		{
			step = step, status = status, message = message, progressRatio = Math.Clamp(progress, 0f, 1f)
		});

	/// <summary>
	/// Runs once before the first beam step: with <c>--create-realm</c>, bootstraps a fresh local realm and
	/// writes the workspace config; otherwise validates the saved login (reusing cid/pid + refreshing the
	/// token) and warns if it's invalid. Never aborts the stack — realm issues are surfaced, not fatal.
	/// </summary>
	private async Task EnsureRealmAndLogin(LocalStackUpCommandArgs args, LocalStackConfig config)
	{
		try
		{
			if (args.createRealm)
			{
				var opts = new RealmSeedOptions
				{
					customerName = args.realmCustomer ?? "beam",
					projectName = args.realmProject ?? "beam-project",
					email = args.realmEmail ?? "beam@beamable.com",
					alias = args.realmAlias ?? "beam-project",
					password = args.realmPassword ?? "123456",
				};
				var realm = await LocalRealmService.CreateRealmAsync(args, config.host, opts);
				Send(string.Empty, "running", $"created local realm cid={realm.cid} pid={realm.pid}", 1f);
			}
			else if (await LocalRealmService.IsLoginValidAsync(args))
			{
				Log.Information("Local login OK.");
			}
			else
			{
				Log.Warning("Local login is invalid (the realm may have been wiped). Re-run `beam local up --create-realm` to bootstrap a fresh realm, or run `beam init`.");
				Send(string.Empty, "running", "local login invalid — re-run with `--create-realm`", 1f);
			}
		}
		catch (Exception e)
		{
			// Don't tear down an otherwise-healthy stack over a realm/login problem — surface it and continue.
			Log.Warning($"Realm/login setup issue: {e.Message}");
		}
	}

	private static string ResolveJavaHome(CommandArgs args, LocalStackConfig config)
	{
		var fromOption = args.AppContext?.JavaPath;
		if (!string.IsNullOrWhiteSpace(fromOption)) return fromOption;
		if (JavaPathOption.TryGetJavaHome(out var home, out _)) return home;
		return config.javaHome; // may be null; the Scala launch shell will fail clearly if a JDK is needed and missing
	}

	// ----------------------------------------------------------------------------------
	// Launching
	// ----------------------------------------------------------------------------------

	private class Launched
	{
		public LocalStackStep Step;
		public Process Process;
		public Task ExitedTask;
		public string StdoutLog;
		public string StderrLog;
		public string WorkingDirectory;
		public string Kind;
	}

	private (string exe, string[] leading) ResolveBeam(CommandArgs args)
	{
		// Run the SAME beam build as a subprocess. When hosted by `dotnet` (dev: `dotnet Beamable.Tools.dll`),
		// invoke that exact dll directly rather than `dotnet beam` (which resolves through the tool cache / cwd).
		var processPath = Environment.ProcessPath ?? string.Empty;
		var isDotnetHost = processPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
		                   || processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);

		if (!isDotnetHost)
			return (processPath, Array.Empty<string>());

		var entryDll = Assembly.GetEntryAssembly()?.Location;
		if (!string.IsNullOrEmpty(entryDll) && File.Exists(entryDll))
			return (processPath, new[] { entryDll });

		return (args.AppContext.DotnetPath, new[] { "beam" });
	}

	private Launched StartStep(LocalStackStep step, LocalStackConfig config, string beamExe, string[] beamLeading,
		string beamWorkspaceFallback, string logsDir)
	{
		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		var argsText = LocalStackConfigIO.Substitute(step.arguments, config) ?? string.Empty;

		// beam sub-commands need to run inside a .beamable workspace to see the local service manifest.
		if (step.beam && (string.IsNullOrEmpty(workDir) || !Directory.Exists(workDir)))
			workDir = beamWorkspaceFallback;

		var safe = SafeName(step.name);
		var stdoutLog = Path.Combine(logsDir, safe + ".log");
		var stderrLog = Path.Combine(logsDir, safe + ".err.log");
		// Fresh log files for this run so readiness reads only the current lifetime.
		File.WriteAllText(stdoutLog, string.Empty);
		File.WriteAllText(stderrLog, string.Empty);

		var kind = step.beam ? "beam"
			: string.Equals(step.command, "docker", StringComparison.OrdinalIgnoreCase) ? "docker"
			: step.shell ? "shell"
			: "process";

		var inner = BuildInnerScript(step, beamExe, beamLeading, argsText, workDir);
		var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };

		if (!OperatingSystem.IsWindows())
		{
			var launcher = Path.Combine(logsDir, safe + ".launch.sh");
			File.WriteAllText(launcher, inner + "\n");
			psi.FileName = "/bin/sh";
			psi.ArgumentList.Add("-c");
			// Daemonize the child so it survives `up` returning (detached, foreground terminal):
			//   exec  → the tracked pid stays == the service
			//   nohup → immune to terminal SIGHUP
			//   < /dev/null → detach stdin so the backgrounded child never reads the terminal (macOS nohup,
			//                 unlike Linux, does NOT redirect stdin — without this, npm/vite/dotnet get SIGTTIN
			//                 or an interactive-stdin surprise and die shortly after `up` exits)
			//   > log 2> err → logs persist after this CLI process exits
			psi.ArgumentList.Add($"exec nohup sh {Sq(launcher)} < /dev/null > {Sq(stdoutLog)} 2> {Sq(stderrLog)}");
		}
		else
		{
			var launcher = Path.Combine(logsDir, safe + ".launch.cmd");
			File.WriteAllText(launcher, inner + "\r\n");
			psi.FileName = "cmd.exe";
			// Windows: best-effort — not fully detached (dies with the console), but still logs to files and
			// detaches stdin from the console so the child doesn't block on / read it.
			psi.Arguments = $"/c \"\"{launcher}\" < NUL > \"{stdoutLog}\" 2> \"{stderrLog}\"\"";
		}

		if (!string.IsNullOrEmpty(workDir) && Directory.Exists(workDir))
			psi.WorkingDirectory = workDir;

		foreach (var (k, v) in step.environment)
			psi.Environment[k] = LocalStackConfigIO.Substitute(v, config);

		// Beam microservice/extension runtimes shell out to `beam generate-env` on startup. By default that is
		// `dotnet tool run beam` (the workspace's LOCAL tool), which fails with "Execute dotnet tool restore ..."
		// when the tool manifest isn't restored. Point BEAM_PATH at THIS beam build so the child reuses it
		// (`dotnet <this-dll> generate-env ...`) instead of the local tool.
		if (step.beam)
		{
			var entryDll = Assembly.GetEntryAssembly()?.Location;
			if (!string.IsNullOrEmpty(entryDll) && File.Exists(entryDll))
				psi.Environment["BEAM_PATH"] = entryDll.Contains(' ') ? $"\"{entryDll}\"" : entryDll;
		}

		Process proc;
		try
		{
			proc = Process.Start(psi);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to start step '{step.name}' ({psi.FileName}): {e.Message}");
		}

		if (proc == null)
			throw new CliException($"Failed to start step '{step.name}' ({psi.FileName}).");

		var exitTcs = new TaskCompletionSource();
		proc.EnableRaisingEvents = true;
		proc.Exited += (_, _) => exitTcs.TrySetResult();

		return new Launched
		{
			Step = step, Process = proc, ExitedTask = exitTcs.Task,
			StdoutLog = stdoutLog, StderrLog = stderrLog, WorkingDirectory = workDir, Kind = kind
		};
	}

	/// <summary>Builds the shell body written to the per-step launcher file (its last command is exec'd so the
	/// tracked pid becomes the service).</summary>
	private static string BuildInnerScript(LocalStackStep step, string beamExe, string[] beamLeading, string argsText,
		string workDir)
	{
		if (step.shell)
			return argsText; // already a shell script; the Scala launcher ends with its own `exec`.

		IEnumerable<string> parts;
		if (step.beam)
			parts = new[] { beamExe }.Concat(beamLeading).Concat(SplitArgs(argsText));
		else
			parts = new[] { ResolveExecutable(step.command, workDir) }.Concat(SplitArgs(argsText));

		if (OperatingSystem.IsWindows())
			return string.Join(" ", parts.Select(WinQuote));

		return "exec " + string.Join(" ", parts.Select(Sq));
	}

	private static string SafeName(string name)
	{
		var cleaned = Regex.Replace(name ?? "step", @"[^A-Za-z0-9._-]+", "_").Trim('_');
		return string.IsNullOrEmpty(cleaned) ? "step" : cleaned;
	}

	/// <summary>Single-quotes a value for <c>/bin/sh</c>.</summary>
	private static string Sq(string s) => "'" + (s ?? string.Empty).Replace("'", "'\\''") + "'";

	private static string WinQuote(string s) =>
		string.IsNullOrEmpty(s) ? "\"\"" : (s.Contains(' ') || s.Contains('"') ? "\"" + s.Replace("\"", "\\\"") + "\"" : s);

	private static string ResolveExecutable(string command, string workDir)
	{
		if (string.IsNullOrWhiteSpace(command))
			throw new CliException("A non-beam/non-shell step must define a 'command'.");

		if (!string.IsNullOrEmpty(workDir) &&
		    (command.StartsWith("./") || command.StartsWith(".\\") || command.Contains('/') || command.Contains('\\')))
		{
			var combined = Path.GetFullPath(Path.Combine(workDir, command));
			if (File.Exists(combined))
				return combined;
		}

		return command;
	}

	private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

	private static IEnumerable<string> SplitArgs(string value) =>
		string.IsNullOrWhiteSpace(value)
			? Array.Empty<string>()
			: Whitespace.Split(value.Trim());

	// ----------------------------------------------------------------------------------
	// Readiness / completion
	// ----------------------------------------------------------------------------------

	private Task AwaitStep(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress, int totalSteps,
		CancellationToken token, Func<Launched> relaunch)
	{
		if (step.waitForExit)
			return AwaitCompletion(step, l, baseProgress);

		if (!string.IsNullOrEmpty(step.readyWhenHttpOk)
		    || !string.IsNullOrEmpty(step.readyWhenHttp200)
		    || !string.IsNullOrEmpty(step.readyWhenLogContains))
			return AwaitReadiness(step, l, config, baseProgress, token, relaunch);

		return AwaitBriefLiveness(step, l, baseProgress, totalSteps, token);
	}

	/// <summary>
	/// For steps with no readiness gate (e.g. beam microservice/extension runs), wait a short grace period and
	/// surface an immediate exit as a failure with its last log line — so a service that dies on startup is
	/// visible instead of being reported as "assuming up".
	/// </summary>
	private async Task AwaitBriefLiveness(LocalStackStep step, Launched l, float baseProgress, int totalSteps,
		CancellationToken token)
	{
		try { await Task.WhenAny(l.ExitedTask, Task.Delay(TimeSpan.FromSeconds(3), token)); }
		catch (OperationCanceledException) { return; }

		if (l.ExitedTask.IsCompleted)
		{
			var code = SafeExitCode(l);
			Send(step.name, "failed", $"exited on startup (code {code}) — see `beam local logs`", baseProgress);
			Log.Warning($"[{step.name}] exited on startup (code {code}). Last log: {LastLogLine(l)}");
			return;
		}

		Send(step.name, "running", "no readiness gate; assuming up", baseProgress + 1f / totalSteps);
	}

	private async Task AwaitCompletion(LocalStackStep step, Launched l, float baseProgress)
	{
		Send(step.name, "starting", "waiting for completion", baseProgress);
		var timeout = TimeSpan.FromSeconds(Math.Max(1, step.readyTimeoutSeconds));
		var done = await Task.WhenAny(l.ExitedTask, Task.Delay(timeout));
		if (done != l.ExitedTask)
		{
			Send(step.name, "failed", $"did not complete within {step.readyTimeoutSeconds}s", baseProgress);
			Log.Warning($"[{step.name}] did not complete within {step.readyTimeoutSeconds}s — continuing.");
			return;
		}

		var code = SafeExitCode(l);
		if (code != 0)
			throw new CliException($"Step '{step.name}' exited with code {code}. Last log: {LastLogLine(l)}");
		Send(step.name, "ready", "completed", baseProgress);
	}

	private async Task AwaitReadiness(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress,
		CancellationToken token, Func<Launched> relaunch)
	{
		var httpUrl = LocalStackConfigIO.Substitute(step.readyWhenHttpOk, config);
		var http200Url = LocalStackConfigIO.Substitute(step.readyWhenHttp200, config);
		var timeout = Math.Max(1, step.readyTimeoutSeconds);
		var retriesLeft = Math.Max(0, step.readyRetries);
		Send(step.name, "starting", $"waiting for readiness (timeout {timeout}s)", baseProgress);

		// One-time diagnostic for the classic macOS trap: AirPlay Receiver squats :5000 and answers the
		// gateway's /health with a 403, so readiness can never pass (and the gateway can't bind the port).
		await WarnIfForeignServer(!string.IsNullOrEmpty(http200Url) ? http200Url : httpUrl, step.name, token);

		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
		var outTail = new LineTailer(l.StdoutLog, -1);
		var errTail = new LineTailer(l.StderrLog, -1);
		try
		{
			var lastLine = "";
			var waited = 0;
			var nextBeat = 10;

			while (waited < timeout)
			{
				token.ThrowIfCancellationRequested();

				if (l.ExitedTask.IsCompleted)
				{
					var code = SafeExitCode(l);
					if (retriesLeft > 0 && relaunch != null)
					{
						retriesLeft--;
						Log.Warning($"[{step.name}] exited early (code {code}); retrying ({retriesLeft} left). Last log: {LastLogLine(l)}");
						Send(step.name, "starting", $"exited early (code {code}) — retrying ({retriesLeft} left)", baseProgress);
						try { await Task.Delay(3000, token); }
						catch (OperationCanceledException) { return; }

						// Relaunch and re-watch the fresh log files (StartStep truncates them).
						l = relaunch();
						outTail.Dispose();
						errTail.Dispose();
						outTail = new LineTailer(l.StdoutLog, -1);
						errTail = new LineTailer(l.StderrLog, -1);
						lastLine = "";
						waited = 0;
						nextBeat = 10;
						continue;
					}

					Send(step.name, "failed", $"process exited early (code {code})", baseProgress);
					Log.Warning($"[{step.name}] exited before becoming ready (code {code}). Last log: {LastLogLine(l)}");
					return;
				}

				// Log-substring gate: scan any lines that appeared since the last poll (in either stream).
				if (!string.IsNullOrEmpty(step.readyWhenLogContains))
				{
					foreach (var line in outTail.ReadAvailableLines().Concat(errTail.ReadAvailableLines()))
					{
						lastLine = line;
						if (line.Contains(step.readyWhenLogContains))
						{
							Send(step.name, "ready", $"ready after {waited}s", baseProgress);
							Log.Information($"[{step.name}] ready after {waited}s.");
							return;
						}
					}
				}

				if (!string.IsNullOrEmpty(http200Url) && await HttpStatusOk(http, http200Url, require200: true, token))
				{
					Send(step.name, "ready", $"ready after {waited}s (200)", baseProgress);
					Log.Information($"[{step.name}] ready after {waited}s (HTTP 200).");
					return;
				}

				if (!string.IsNullOrEmpty(httpUrl) && await HttpStatusOk(http, httpUrl, require200: false, token))
				{
					Send(step.name, "ready", $"ready after {waited}s", baseProgress);
					Log.Information($"[{step.name}] ready after {waited}s.");
					return;
				}

				await Task.Delay(1000, token);
				waited++;

				if (waited >= nextBeat)
				{
					nextBeat += 10;
					var hint = string.IsNullOrEmpty(lastLine) ? "" : $" | {Trim(lastLine, 110)}";
					Send(step.name, "starting", $"still starting — {waited}/{timeout}s{hint}", baseProgress);
					Log.Information($"[{step.name}] still starting — {waited}/{timeout}s{hint}");
				}
			}

			Send(step.name, "running", $"did not signal ready within {timeout}s; continuing", baseProgress);
			Log.Warning($"[{step.name}] did not signal ready within {timeout}s — continuing anyway.");
		}
		finally
		{
			outTail.Dispose();
			errTail.Dispose();
		}
	}

	/// <summary>
	/// True if the step's HTTP readiness endpoint is already answering — i.e. something is already serving
	/// there, so launching another instance would just conflict on the port. Only meaningful for HTTP-gated
	/// steps (gateway, portal); log-gated / no-gate steps return false (their duplicate detection is the
	/// run-state pid check).
	/// </summary>
	/// <summary>
	/// Warns when a readiness endpoint is already answered by a foreign server — most commonly macOS AirPlay
	/// Receiver squatting :5000 (Server: AirTunes), which both blocks the gateway from binding the port and
	/// makes the /health readiness gate impossible to satisfy.
	/// </summary>
	private static async Task WarnIfForeignServer(string url, string stepName, CancellationToken token)
	{
		if (string.IsNullOrEmpty(url)) return;
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
			using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
			var server = res.Headers.Server?.ToString() ?? "";
			if ((int)res.StatusCode != 200 && server.Contains("AirTunes", StringComparison.OrdinalIgnoreCase))
			{
				Log.Warning($"[{stepName}] {url} is being answered by macOS AirPlay Receiver (Server: {server}), not your service. " +
				            "Port 5000 is taken — turn OFF System Settings → General → AirDrop & Handoff → \"AirPlay Receiver\" " +
				            "(or change the gateway port), then re-run.");
			}
		}
		catch { /* diagnostic only */ }
	}

	private static async Task<bool> AlreadyServing(LocalStackStep step, LocalStackConfig config, CancellationToken token)
	{
		var http200Url = LocalStackConfigIO.Substitute(step.readyWhenHttp200, config);
		var httpUrl = LocalStackConfigIO.Substitute(step.readyWhenHttpOk, config);
		if (string.IsNullOrEmpty(http200Url) && string.IsNullOrEmpty(httpUrl))
			return false;

		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
		if (!string.IsNullOrEmpty(http200Url) && await HttpStatusOk(http, http200Url, require200: true, token))
			return true;
		if (!string.IsNullOrEmpty(httpUrl) && await HttpStatusOk(http, httpUrl, require200: false, token))
			return true;
		return false;
	}

	private static async Task<bool> HttpStatusOk(HttpClient http, string url, bool require200, CancellationToken token)
	{
		try
		{
			using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
			return !require200 || (int)res.StatusCode == 200;
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			throw;
		}
		catch
		{
			return false;
		}
	}

	private static int SafeExitCode(Launched l)
	{
		try { return l.Process.HasExited ? l.Process.ExitCode : 0; }
		catch { return 0; }
	}

	private static string LastLogLine(Launched l)
	{
		foreach (var file in new[] { l.StderrLog, l.StdoutLog })
		{
			try
			{
				if (!File.Exists(file)) continue;
				var last = File.ReadLines(file).LastOrDefault(x => !string.IsNullOrWhiteSpace(x));
				if (!string.IsNullOrEmpty(last)) return Trim(last, 200);
			}
			catch { /* best-effort */ }
		}

		return "";
	}

	private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

	// ----------------------------------------------------------------------------------
	// Attach (foreground tail)
	// ----------------------------------------------------------------------------------

	private async Task TailToConsole(LocalStackRunState runState, CancellationToken token)
	{
		List<LocalStackRunEntry> snapshot;
		lock (_launchedLock) snapshot = runState.steps.ToList();

		var tasks = new List<Task>();
		foreach (var e in snapshot)
		{
			if (!string.IsNullOrEmpty(e.stdoutLog)) tasks.Add(FollowFile(e.stdoutLog, e.name, isError: false, token));
			if (!string.IsNullOrEmpty(e.stderrLog)) tasks.Add(FollowFile(e.stderrLog, e.name, isError: true, token));
		}

		try { await Task.WhenAll(tasks); }
		catch (OperationCanceledException) { /* expected on detach */ }
	}

	private static bool IsPidAlive(int pid)
	{
		if (pid <= 0) return false;
		try { return !Process.GetProcessById(pid).HasExited; }
		catch { return false; }
	}

	private static async Task FollowFile(string path, string name, bool isError, CancellationToken token)
	{
		try
		{
			using var tailer = new LineTailer(path, 0); // start at end: only new lines while attached
			while (!token.IsCancellationRequested)
			{
				foreach (var line in tailer.ReadAvailableLines())
				{
					if (isError) Log.Warning($"[{name}] {line}");
					else Log.Information($"[{name}] {line}");
				}

				await Task.Delay(400, token);
			}
		}
		catch (OperationCanceledException) { /* expected */ }
		catch (Exception e) { Log.Verbose($"[{name}] tail stopped: {e.Message}"); }
	}

	// ----------------------------------------------------------------------------------
	// Teardown (failure path only — normal/cancelled exit leaves the stack running)
	// ----------------------------------------------------------------------------------

	private void TearDown(List<Launched> launched)
	{
		List<Launched> snapshot;
		lock (_launchedLock) snapshot = launched.ToList();
		if (snapshot.Count == 0) return;

		for (var i = snapshot.Count - 1; i >= 0; i--)
		{
			var l = snapshot[i];
			try
			{
				if (l.Process.HasExited) continue;
				l.Process.Kill(entireProcessTree: true);
				Log.Information($"[{l.Step.name}] stopped");
			}
			catch (Exception e)
			{
				Log.Warning($"[{l.Step.name}] failed to stop: {e.Message}");
			}
		}
	}

	private static int SafePid(Launched l)
	{
		try { return l.Process.Id; }
		catch { return 0; }
	}
}
