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
	public bool runDetached;
	public bool build;
	public bool saveLogs;
	public bool noCreateRealm;
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
		var detach = new Option<bool>("--run-detached", "Run the stack detached: services keep running after `up` returns; manage with `beam local ps`/`logs`/`stop`. Default runs attached — logs stream live and the stack stops when `up` exits (Ctrl+C)");
		detach.AddAlias("--detach");
		detach.AddAlias("-d");
		AddOption(detach, (args, v) => args.runDetached = v);

		AddOption(new Option<bool>("--build", "Rebuild the C# hosts, Scala services, and portal deps before launching (a manifest that declares a build output also builds that step on its own when the output is missing; microservices/extensions always build via project run)"),
			(args, v) => args.build = v);

		AddOption(new Option<bool>("--save-logs", "Persist per-run logs under the workspace (.beamable/local-stack-logs/run-<id>); without it logs go to a temp folder and are removed on `beam local stop`"),
			(args, v) => args.saveLogs = v);

		AddOption(new Option<bool>("--no-create-realm", "Do not auto-create a local realm when the saved login is invalid — just warn (by default `up` creates one after a docker cleanup)"),
			(args, v) => args.noCreateRealm = v);
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

		// A build step that declares an output runs even without --build when that output is missing: the run
		// step after it launches a PRE-BUILT binary, so without it `up` would retry a nonexistent executable
		// `readyRetries` times and then report the stack as up minus that service. Mirrors the reference
		// scripts/run-local-stack.sh, which builds each gateway/worker binary when it isn't there.
		var autoBuild = args.build
			? new HashSet<LocalStackStep>()
			: config.steps.Where(s => s.enabled && LocalStackConfigIO.BuildOutputMissing(s, config)).ToHashSet();

		bool Included(LocalStackStep s) =>
			s.enabled
			&& (!s.build || args.build || autoBuild.Contains(s)) // build steps run under --build, or when their output is missing
			&& (only == null || only.Contains(s.name))
			&& (skip == null || !skip.Contains(s.name));

		var steps = config.steps.Where(Included).ToList();
		if (steps.Count == 0)
			throw new CliException("No steps to run (all disabled or filtered out).");

		// Say WHY a build is running when the user didn't ask for one, so it never looks like a mystery step.
		// The per-step reason also rides along on the stream message when it launches (see the loop below).
		foreach (var s in steps.Where(autoBuild.Contains))
			Log.Information($"[{s.name}] {LocalStackConfigIO.ResolveRequiredOutput(s, config)} is missing — building it (pass --build to rebuild everything).");

		// Manifests generated before build steps existed won't have any — tell the user how to get them.
		if (args.build && !config.steps.Any(s => s.build))
			Log.Warning("--build was passed but this manifest has no build steps. Re-run `beam local init` to regenerate it with build steps (or add them by hand).");

		// The self-healing build and the port pre-flight are both driven by manifest fields (`requiredOutput`,
		// `port`) that only `beam local init` writes. A manifest generated before them gets neither, so say so
		// rather than letting the user believe protections are running that aren't.
		if (!config.steps.Any(s => s.build && !string.IsNullOrWhiteSpace(s.requiredOutput))
		    && !config.steps.Any(s => s.port > 0))
			Log.Warning("This manifest predates automatic builds and port-conflict checks (no `requiredOutput` or `port` on any step), so neither will run. Re-run `beam local init` to regenerate it.");

		// A clean build should re-resolve deps too: drop the shared Scala classpath cache so each service's
		// launch rebuilds it (the cache otherwise only refreshes when core/pom.xml changes).
		if (args.build)
			TryDeleteDir(Path.Combine(Path.GetTempPath(), "beam-scala-cp"));

		// Fail fast if the stack needs Docker but its daemon isn't running — otherwise the first docker step
		// dies mid-bring-up with a cryptic daemon error.
		EnsureDockerRunning(steps);

		// Free the Mongo host port if a foreign process is squatting 127.0.0.1:27015. Docker publishes mongo_master on 0.0.0.0/[::], but a specific
		// 127.0.0.1 bind shadows it for `localhost` connections, timing out the gateway + every scala service.
		FreeMongoPortIfSquatted(steps);

		// Resolve how to invoke this same beam CLI for `beam: true` steps, and the workspace to run them from.
		var (beamExe, beamLeading) = ResolveBeam(args);
		var beamWorkspaceFallback = args.ConfigService?.BeamableWorkspace
		                            ?? args.ConfigService?.WorkingDirectory
		                            ?? Directory.GetCurrentDirectory();

		// Default is attached: stream each step's output live and stop the stack when `up` exits. --run-detached
		// keeps the file-based model (services survive `up`, managed via ps/logs/stop).
		var attached = !args.runDetached;

		var runStatePath = LocalStackRunStateIO.ResolveRunStatePath(path);
		// A unique per-run dir (temp by default, workspace under --save-logs) holding launcher scripts and — in
		// detached or --save-logs runs — the log files. Unique paths mean a fresh run never collides with a log a
		// leftover wrapper from a crashed run still holds open.
		var logsDir = LocalStackRunStateIO.ResolveRunLogsDir(path, args.saveLogs);
		Directory.CreateDirectory(logsDir);

		// Idempotency + run-state are only meaningful for detached stacks (they outlive `up`). Attached runs are
		// always fresh and torn down on exit, so they carry nothing over and write no run-state.
		var aliveByName = new Dictionary<string, LocalStackRunEntry>(StringComparer.OrdinalIgnoreCase);
		LocalStackRunState runState = null;
		if (args.runDetached)
		{
			// Carry over any steps from a previous detached `up` that are still running (alive pid), and skip
			// re-launching them below — lets you re-run `up` (e.g. to add the portal) without restarting the
			// Scala backend, and avoids duplicate processes fighting over the same ports.
			var existing = LocalStackRunStateIO.Load(runStatePath);
			if (existing?.steps != null)
			{
				foreach (var e in existing.steps)
				{
					// Identity-checked, not just pid-alive: a recycled pid used to read as "already running" and
					// silently cost the stack that service — see LocalStackLiveness.
					var liveness = LocalStackLiveness.Check(e, LocalStackStopCommand.BuildKillTokens(e));
					if (liveness == LocalStackLiveness.Liveness.Stopped)
						continue;

					// Unverified carries over: relaunching a service that IS still up would fight it for its port
					// (and duplicate a JVM), which is worse than skipping a service that has actually died — the
					// next `up` re-checks, and `ps` shows it.
					if (liveness == LocalStackLiveness.Liveness.Unverified)
						Log.Verbose($"[{e.name}] pid {e.pid} could not be confirmed; assuming it is still running.");

					aliveByName[e.name] = e;
				}
			}
			runState = new LocalStackRunState
			{
				host = config.host, portalUrl = config.portalUrl,
				logsDir = logsDir, ephemeralLogs = !args.saveLogs,
				steps = aliveByName.Values.ToList()
			};
			LocalStackRunStateIO.Save(runStatePath, runState);
		}

		// Clear temp launcher/log dirs left by previous (crashed/detached) runs, keeping this run's dir and any
		// dir a carried-over live service still logs to. Keeps the temp folder from growing across runs.
		PruneStaleTempLogs(path, logsDir, aliveByName.Values.Select(e => Path.GetDirectoryName(e.stdoutLog)));

		var launched = new List<Launched>();
		var token = args.Lifecycle.CancellationToken;
		var realmEnsured = false;

		// Port pre-flight, BEFORE anything is launched and before the try/catch below. A squatted port is
		// otherwise invisible until the service loses the bind and shows up as a readiness timeout, a Caddy 502,
		// or dependents failing to fetch dbids — so it is worth stopping for. It runs here, not in the launch
		// loop, for two reasons: nothing has started yet (so aborting cannot orphan half a stack, nor clear the
		// run-state that is the only record of carried-over services still running from a previous `up`), and
		// every conflict is reported at once instead of one per run.
		await EnsurePortsFree(steps, config, aliveByName, token);

		// Attached: put every launched process in a kill-on-close job so the whole stack dies with the CLI —
		// even on a terminal close / IDE stop / hard kill that skips the graceful teardown (Windows only; no-op
		// elsewhere). Held for the command's lifetime; disposed in the teardown paths below.
		var job = attached ? LocalStackJobObject.CreateKillOnClose() : null;

		// Launch a step and record it in the run-state (upsert by name, so a retry replaces the dead entry
		// rather than duplicating it). Returned as a local function so readiness can relaunch on early exit.
		Launched LaunchAndRegister(LocalStackStep step)
		{
			var l = StartStep(step, config, beamExe, beamLeading, beamWorkspaceFallback, logsDir, attached, args.saveLogs, job);
			lock (_launchedLock)
			{
				launched.Add(l);

				// Build steps compile a component and exit; they are not a running service, so they are not
				// recorded in the run-state (nothing for ps/stop to track). AwaitStep still waits on completion.
				if (step.build)
				{
					Log.Information($"[{step.name}] building (pid={SafePid(l)})");
					return l;
				}

				// Attached runs keep nothing across invocations — no run-state to write.
				if (attached)
				{
					Log.Information($"[{step.name}] started (pid={SafePid(l)})");
					return l;
				}

				var entry = runState.steps.FirstOrDefault(e => e.name == step.name);
				if (entry == null)
				{
					entry = new LocalStackRunEntry { name = step.name };
					runState.steps.Add(entry);
				}

				entry.group = step.group;
				entry.pid = SafePid(l);
				// Pin the pid to this exact process, so a later `up`/`ps`/`stop` can tell it apart from whatever
				// recycles that pid number after the stack dies.
				entry.startedAtUtcTicks = LocalStackLiveness.StartTicksOf(entry.pid);
				entry.matchToken = LocalStackConfigIO.Substitute(step.mainClass, config);
				entry.kind = l.Kind;
				entry.stdoutLog = l.StdoutLog;
				entry.stderrLog = l.StderrLog;
				entry.workingDirectory = l.WorkingDirectory;
				entry.command = step.command;
				entry.stopArguments = LocalStackConfigIO.Substitute(step.stopArguments, config);
 				entry.purgeStopArguments = LocalStackConfigIO.Substitute(step.purgeStopArguments, config);
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

					// Can this step even start? A missing working directory (a build output folder whose build
					// failed or timed out, or an unedited `<EDIT: ...>` placeholder) means it cannot. Report the
					// step as failed and keep going: one unusable step is not a reason to tear down a stack that
					// is otherwise coming up, and the old behavior — launching it anyway — just produced a raw
					// "not recognized" several retries later.
					var blocked = CannotStartReason(step, config);
					if (blocked != null)
					{
						Send(step.name, "failed", blocked, baseProgress);
						Log.Warning($"[{step.name}] {blocked}");
						continue;
					}

					// Ensure a valid local realm/login before the first beam step — microservices and
					// extensions authenticate against the local backend on startup.
					if (step.beam && !realmEnsured)
					{
						realmEnsured = true;
						await EnsureRealmAndLogin(args, config);
					}

					// An unrequested build is surprising unless it says why, and the stream channel is what the
					// user (and the Unity/portal tooling) actually sees.
					Send(step.name, "starting", autoBuild.Contains(step)
						? $"launching ({idx + 1}/{steps.Count}) — its output is missing, so it builds even without --build"
						: $"launching ({idx + 1}/{steps.Count})", baseProgress);

					var stepToRun = step;
					var l = LaunchAndRegister(stepToRun);
					awaits.Add(AwaitStep(stepToRun, l, config, baseProgress, steps.Count, token,
						() => LaunchAndRegister(stepToRun)));

					// Within a large parallel group (the "scala" batch), space out launches so ~34 JVMs don't
					// storm the Mongo port-proxy at once. Skip the delay after the batch's last launch.
					if (batch.Count > 1 && idx != batch[^1])
						await Task.Delay(GroupLaunchStagger, token);
				}

				await Task.WhenAll(awaits);
			}

			// Detached only: on Windows the tracked pid is the cmd.exe wrapper, not the service (cmd → powershell
			// → java). Rewrite each recorded pid to the real service leaf so `ps`/`stop` (and the next `up`'s
			// idempotency check) act on the JVM instead of a wrapper that will die. Attached keeps no run-state.
			if (args.runDetached)
				ReconcileLeafPids(runState, runStatePath, launched);

			// No beam steps ran the hook above — still ensure/validate the realm once the backend is up.
			if (!realmEnsured)
				await EnsureRealmAndLogin(args, config);

			Send(string.Empty, "running",
				$"Stack is up. Backend={config.host} Portal={config.portalUrl}.", 1f);
			Log.Information($"Stack is up. Backend={config.host}  Portal={config.portalUrl}");

			if (args.runDetached)
			{
				// Fire-and-return (docker compose up -d style). NOTE: don't use this from an IDE run-config —
				// when this process exits the IDE ends the run session and kills the child process tree.
				Log.Information("Detached — the stack keeps running. Manage it with: beam local ps | beam local logs | beam local stop");
			}
			else
			{
				// Attached (default): logs already stream live from each child. Hold the foreground until the
				// user stops (Ctrl+C) or all long-running services exit, then tear the whole stack down.
				Log.Information("Attached — streaming logs. Press Ctrl+C to stop the stack.");
				try { await WaitAttached(launched, token); }
				catch (OperationCanceledException) { /* Ctrl+C — fall through to teardown */ }
				Log.Information("Stopping the local stack...");
				TearDown(launched);
				if (!args.saveLogs) TryDeleteDir(logsDir);
			}
		}
		catch (OperationCanceledException)
		{
			if (args.runDetached)
			{
				// Detached: cancellation during bring-up leaves the stack running; run-state lets `stop` bring it down.
				Send(string.Empty, "running", "detached — stack left running (use `beam local stop`)", 1f);
				Log.Information("Detached — stack left running. Use `beam local stop` to bring it down.");
			}
			else
			{
				// Attached: Ctrl+C during bring-up tears down what started so nothing is left orphaned.
				Log.Information("Stopping the local stack...");
				TearDown(launched);
				if (!args.saveLogs) TryDeleteDir(logsDir);
			}
		}
		catch (Exception)
		{
			// A genuine bring-up failure: tear down what we started to avoid orphans, and clear detached run-state.
			TearDown(launched);
			if (args.runDetached) LocalStackRunStateIO.Clear(runStatePath);
			// Don't leave an empty temp log dir behind for a run that never came up.
			if (!args.saveLogs) TryDeleteDir(logsDir);
			throw;
		}
		finally
		{
			// Closing the job kills anything still assigned (attached only) — belt-and-suspenders on top of the
			// OS's own kill-on-close when the process exits.
			job?.Dispose();
		}
	}

	/// <summary>
	/// Attached mode: hold the foreground while logs stream, until the user cancels (Ctrl+C) or every
	/// long-running service has exited on its own. Throws <see cref="OperationCanceledException"/> on cancel
	/// so the caller tears the stack down.
	/// </summary>
	private async Task WaitAttached(List<Launched> launched, CancellationToken token)
	{
		List<Task> longRunning;
		lock (_launchedLock)
			longRunning = launched.Where(l => !l.Step.waitForExit).Select(l => l.ExitedTask).ToList();

		if (longRunning.Count == 0)
		{
			// No long-running services to hold the foreground — just wait for Ctrl+C.
			await Task.Delay(Timeout.Infinite, token);
			return;
		}

		await Task.WhenAny(Task.WhenAll(longRunning), Task.Delay(Timeout.Infinite, token));
		token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Fails the whole command — before anything has been launched — when a step's declared port is already held
	/// by something that is not this stack. Steps carried over from a previous detached `up`, and steps whose
	/// readiness endpoint is already answering, are skipped: those are OUR service on that port, and the launch
	/// loop skips them for the same reason. Every conflict is collected so one run reports them all.
	/// </summary>
	private async Task EnsurePortsFree(List<LocalStackStep> steps, LocalStackConfig config,
		Dictionary<string, LocalStackRunEntry> aliveByName, CancellationToken token)
	{
		var conflicts = new List<string>();
		foreach (var step in steps.Where(s => s.port > 0))
		{
			token.ThrowIfCancellationRequested();
			if (aliveByName.ContainsKey(step.name))
			{
				continue; // our own service from a previous `up` owns that port
			}

			if (!LocalStackPortGuard.IsPortTaken(step.port))
			{
				continue;
			}

			// Taken — but by us? A gateway/portal left running outside the run-state answers its own readiness
			// endpoint, and the launch loop will skip launching a duplicate, so that is not a conflict.
			if (await AlreadyServing(step, config, token))
			{
				continue;
			}

			var conflict = LocalStackPortGuard.DescribeConflict(step.name, step.port);
			if (conflict != null)
			{
				conflicts.Add(conflict);
			}
		}

		if (conflicts.Count == 0)
		{
			return;
		}

		foreach (var conflict in conflicts)
		{
			Send(string.Empty, "failed", conflict, 0f);
		}

		throw new CliException(string.Join(Environment.NewLine, conflicts)
		                       + Environment.NewLine
		                       + "Nothing was started, so no part of the stack was touched.");
	}

	/// <summary>
	/// Why a step cannot be launched at all, or null when it can. Only the working directory is checked: every
	/// non-<c>beam</c> step names a real directory (a repo, or a build output folder), and when it is absent the
	/// launcher would silently fall back to the CLI's own cwd and fail with a raw "not recognized". <c>beam</c>
	/// steps are exempt — <see cref="StartStep"/> substitutes the workspace for them.
	/// </summary>
	private static string CannotStartReason(LocalStackStep step, LocalStackConfig config)
	{
		if (step.beam)
		{
			return null;
		}

		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		if (string.IsNullOrEmpty(workDir) || Directory.Exists(workDir))
		{
			return null;
		}

		// Deliberately does NOT tell the user to pass --build: when this is a build output folder, its build step
		// has already run (or been filtered out), so the honest advice is to look at why it produced nothing.
		return $"working directory '{workDir}' does not exist, so this step cannot start. If it is a build "
		       + "output folder, its build step failed, timed out, or was skipped (see `beam local logs`); "
		       + "otherwise fix the path in the manifest (or re-run `beam local init`)";
	}

	/// <summary>Best-effort recursive delete of a directory (used to clean up ephemeral temp log dirs).</summary>
	private static void TryDeleteDir(string dir)
	{
		try { if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
		catch { /* best-effort */ }
	}

	/// <summary>
	/// Deletes stale <c>run-*</c> temp log dirs for this workspace, so temp logs from previous crashed or
	/// detached runs don't accumulate. Keeps <paramref name="currentLogsDir"/> and any dir in
	/// <paramref name="keepDirs"/> (the log dirs of carried-over live services). Best-effort — a dir still
	/// held open by a leftover wrapper is skipped rather than failing the run.
	/// </summary>
	private static void PruneStaleTempLogs(string manifestPath, string currentLogsDir, IEnumerable<string> keepDirs)
	{
		try
		{
			var baseDir = LocalStackRunStateIO.ResolveTempLogsBase(manifestPath);
			if (!Directory.Exists(baseDir)) return;

			var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(currentLogsDir) };
			foreach (var d in keepDirs)
				if (!string.IsNullOrEmpty(d)) keep.Add(Path.GetFullPath(d));

			foreach (var d in Directory.GetDirectories(baseDir))
			{
				if (keep.Contains(Path.GetFullPath(d))) continue;
				try { Directory.Delete(d, recursive: true); }
				catch { /* a live run or leftover wrapper may still hold a handle — skip it */ }
			}
		}
		catch { /* best-effort — never fail bring-up over log cleanup */ }
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
			// Reuse the existing login only if the workspace is already pointed at the local backend and its
			// token still resolves — otherwise (e.g. config points at a dev server) fall through and re-point it.
			if (await LocalRealmService.IsLoginValidAsync(args, config.host))
			{
				Log.Information("Local login OK.");
				return;
			}

			// Invalid (e.g. the realm was wiped by a docker cleanup). By default, bootstrap a fresh realm so
			// the microservices/extensions can authenticate; --no-create-realm turns this into a warning.
			if (args.noCreateRealm)
			{
				Log.Warning("Local login is invalid (the realm may have been wiped). Run `beam init`, or drop --no-create-realm to auto-create one.");
				Send(string.Empty, "running", "local login invalid — create skipped (--no-create-realm)", 1f);
				return;
			}

			Log.Information("Local login is invalid — ensuring the local realm (create if missing, else log in)...");
			var opts = new RealmSeedOptions
			{
				customerName = args.realmCustomer ?? "beam",
				projectName = args.realmProject ?? "beam-project",
				email = args.realmEmail ?? "beam@beamable.com",
				alias = args.realmAlias ?? "beam-project",
				password = args.realmPassword ?? "123456",
			};
			var realm = await LocalRealmService.EnsureRealmAsync(args, config.host, opts);
			Send(string.Empty, "running", $"local realm ready cid={realm.cid} pid={realm.pid}", 1f);
		}
		catch (Exception e)
		{
			// Don't tear down an otherwise-healthy stack over a realm/login problem — surface it and continue.
			Log.Warning($"Realm/login setup issue: {e.Message}");
		}
	}

	/// <summary>
	/// If any step launches docker, verify the Docker <b>daemon</b> is actually running (not just that the CLI
	/// binary exists) before we start anything, so bring-up fails fast with a clear message instead of the
	/// first `docker compose up` dying on a daemon-connection error mid-run.
	/// </summary>
	/// <summary>
	/// Host port the local stack's <c>mongo_master</c> container is published on — mirrors
	/// <c>BeamableAPI/docker-compose.yml</c> (<c>"27015:27017"</c>) and the <c>MongoDB:MainMongoHost</c> =
	/// <c>localhost:27015</c> that the gateway + scala services connect to. Not modeled in CLI config.
	/// </summary>
	private const int MongoHostPort = 27015;

	/// <summary>
	/// Delay between launches within a parallel group (the ~34-service "scala" batch). Launching them all at
	/// once storms Docker Desktop's Windows userspace port-proxy with simultaneous <c>localhost:27015</c> Mongo
	/// connects; a subset stall in CONNECTING, hit the driver's 5s serverSelectionTimeout, and hang (they never
	/// exit, so per-step <c>readyRetries</c> never re-fires). Staggering spreads the connect attempts so each
	/// completes well within the timeout. Readiness waits still overlap — only the launches are spaced out.
	/// </summary>
	private static readonly TimeSpan GroupLaunchStagger = TimeSpan.FromMilliseconds(2000);

	/// <summary>
	///  if a non-Docker process is squatting <c>127.0.0.1:27015</c> it shadows the docker-published mongo_master, so
	/// every host process' <c>localhost:27015</c> Mongo connection times out and the gateway + scala services
	/// die. Freeing it lets those connections fall through to Docker's <c>0.0.0.0</c> bind. Only runs for
	/// Docker-dependent stacks; a no-op on non-Windows.
	/// </summary>
	private static void FreeMongoPortIfSquatted(List<LocalStackStep> steps)
	{
		var needsDocker = steps.Any(s => string.Equals(s.command, "docker", StringComparison.OrdinalIgnoreCase));
		if (!needsDocker) return;

		var freed = LocalStackProcess.FreeLoopbackPortSquatter(MongoHostPort);
		if (freed != null)
			Log.Warning($"Freed '{freed}' off 127.0.0.1:{MongoHostPort} — it was shadowing the local stack's " +
			            "mongo_master; Mongo connections will now reach Docker.");
	}

	private static void EnsureDockerRunning(List<LocalStackStep> steps)
	{
		var needsDocker = steps.Any(s => string.Equals(s.command, "docker", StringComparison.OrdinalIgnoreCase));
		if (!needsDocker) return;

		if (!DockerPathOption.TryGetDockerPath(out var dockerPath, out var err))
			throw new CliException($"This stack needs Docker, but the Docker CLI wasn't found: {err}");

		var psi = new ProcessStartInfo
		{
			FileName = dockerPath,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		// `docker info` talks to the daemon (unlike `docker --version`, which only checks the CLI binary).
		psi.ArgumentList.Add("info");
		psi.ArgumentList.Add("--format");
		psi.ArgumentList.Add("{{.ServerVersion}}");

		try
		{
			var proc = Process.Start(psi);
			if (proc == null)
				throw new CliException("Could not run docker to check whether the daemon is running.");

			if (!proc.WaitForExit(15000))
			{
				try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
				throw new CliException("Docker did not respond within 15s — is Docker Desktop (the daemon) running?");
			}

			if (proc.ExitCode != 0)
				throw new CliException("Docker does not appear to be running. Start Docker Desktop (or the Docker daemon), then re-run `beam local up`.");

			Log.Information("Docker daemon is running.");
		}
		catch (CliException)
		{
			throw;
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to verify Docker is running ({dockerPath}): {e.Message}. Start Docker and re-run `beam local up`.");
		}
	}

	private static string ResolveJavaHome(CommandArgs args, LocalStackConfig config)
	{
		// 1. Explicit --java-path CLI option (user wants this exact path for this invocation)
		var explicitPath = args.AppContext?.ExplicitJavaPath;
		if (!string.IsNullOrWhiteSpace(explicitPath)) return explicitPath;

		// 2. BEAM_JAVA_HOME env var (machine-level explicit override)
		var envHome = ConfigService.CustomJavaHome;
		if (!string.IsNullOrWhiteSpace(envHome)) return envHome;

		// 3. Manifest-pinned path (stored by beam local init — the user's chosen version)
		if (!string.IsNullOrWhiteSpace(config.javaHome)) return config.javaHome;

		// 4. Auto-detection as last resort (JAVA_HOME, macOS java_home, common install dirs)
		if (JavaPathOption.TryGetJavaHome(out var home, out _)) return home;
		return null; // Scala launch shell will fail clearly if a JDK is needed and missing
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
		/// <summary>Attached mode: the live stdout/stderr buffer feeding readiness. Null in detached mode
		/// (readiness tails the log files instead).</summary>
		public StreamLineBuffer LineBuffer;

		/// <summary>The line source(s) the readiness gate should poll: the in-memory stream buffer (attached)
		/// or fresh tailers over the stdout/stderr log files (detached).</summary>
		public IReadOnlyList<ILineSource> OpenLineSources() =>
			LineBuffer != null
				? new ILineSource[] { LineBuffer }
				: new ILineSource[] { new LineTailer(StdoutLog, -1), new LineTailer(StderrLog, -1) };
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
		string beamWorkspaceFallback, string logsDir, bool attached, bool saveLogs, LocalStackJobObject job)
	{
		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		var argsText = LocalStackConfigIO.Substitute(step.arguments, config) ?? string.Empty;

		// `--build` always does a CLEAN Scala build: an incremental `mvn package` can leave cross-module classes
		// skewed (the NoSuchMethodError class of failure). Inject `clean` for manifests generated before this
		// whose mvn build step only says `package`; new manifests already include it.
		if (step.build && IsMvn(step.command) && !argsText.Contains("clean"))
			argsText = InsertCleanBeforeMavenGoal(argsText);

		// beam sub-commands need to run inside a .beamable workspace to see the local service manifest.
		if (step.beam && (string.IsNullOrEmpty(workDir) || !Directory.Exists(workDir)))
			workDir = beamWorkspaceFallback;

		// A step whose working directory is missing cannot run at all; the launch loop checks that up front
		// (CannotStartReason) and reports the step as failed rather than launching it into the CLI's own cwd.

		var safe = SafeName(step.name);
		var stdoutLog = Path.Combine(logsDir, safe + ".log");
		var stderrLog = Path.Combine(logsDir, safe + ".err.log");
		// Detached (and attached --save-logs) redirect/tee to log files, so reset them for this run's lifetime.
		// Attached without --save-logs pipes straight to the console and writes no log files.
		if (!attached || saveLogs)
		{
			ResetLog(stdoutLog);
			ResetLog(stderrLog);
		}

		var kind = step.beam ? "beam"
			: string.Equals(step.command, "docker", StringComparison.OrdinalIgnoreCase) ? "docker"
			: step.shell ? "shell"
			: "process";

		var inner = BuildInnerScript(step, beamExe, beamLeading, argsText, workDir);
		var launcher = WriteLauncher(step, logsDir, safe, inner, argsText);
		var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };

		if (!OperatingSystem.IsWindows())
		{
			psi.FileName = "/bin/sh";
			psi.ArgumentList.Add("-c");
			// Attached: run the launcher as a normal foreground child (dies with `up`); pipe its output to us.
			// Detached: daemonize so it survives `up` returning —
			//   exec → tracked pid stays == the service; nohup → immune to SIGHUP; < /dev/null → detach stdin
			//   (macOS nohup doesn't redirect stdin); > log 2> err → logs persist after this CLI exits.
			psi.ArgumentList.Add(attached
				? $"exec sh {Sq(launcher)}"
				: $"exec nohup sh {Sq(launcher)} < /dev/null > {Sq(stdoutLog)} 2> {Sq(stderrLog)}");
		}
		else
		{
			psi.FileName = "cmd.exe";
			// Attached: run the launcher and pipe stdout/stderr back (redirected below). Detached: redirect to
			// the log files and detach stdin from the console.
			psi.Arguments = attached
				? $"/c \"\"{launcher}\"\""
				: $"/c \"\"{launcher}\" < NUL > \"{stdoutLog}\" 2> \"{stderrLog}\"\"";
		}

		if (attached)
		{
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardInput = true; // closed after start so children never block reading the console
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

		// Attached: enroll in the kill-on-close job immediately — before the child spawns grandchildren, so the
		// whole subtree (powershell → java, etc.) inherits membership and is reaped when the CLI exits.
		job?.Assign(proc);

		var exitTcs = new TaskCompletionSource();
		proc.EnableRaisingEvents = true;
		proc.Exited += (_, _) => exitTcs.TrySetResult();

		StreamLineBuffer buffer = null;
		if (attached)
		{
			// Detach stdin so the child never blocks reading the console.
			try { proc.StandardInput.Close(); } catch { /* some children don't open stdin */ }

			// Pipe stdout/stderr live: to the console (prefixed), to the readiness buffer, and — under
			// --save-logs — tee to the log files. No file is required for the stack to run.
			buffer = new StreamLineBuffer();
			_ = PumpAsync(proc.StandardOutput, step.name, isError: false, buffer, saveLogs ? stdoutLog : null);
			_ = PumpAsync(proc.StandardError, step.name, isError: true, buffer, saveLogs ? stderrLog : null);
		}

		return new Launched
		{
			Step = step, Process = proc, ExitedTask = exitTcs.Task,
			StdoutLog = stdoutLog, StderrLog = stderrLog, WorkingDirectory = workDir, Kind = kind,
			LineBuffer = buffer
		};
	}

	/// <summary>Writes the per-step launcher script to <paramref name="logsDir"/> and returns its path. On
	/// Windows a PowerShell shell step gets a <c>.launch.ps1</c> plus a <c>.cmd</c> shim that invokes it; every
	/// other case writes the inner command directly.</summary>
	private static string WriteLauncher(LocalStackStep step, string logsDir, string safe, string inner, string argsText)
	{
		if (!OperatingSystem.IsWindows())
		{
			var sh = Path.Combine(logsDir, safe + ".launch.sh");
			File.WriteAllText(sh, inner + "\n");
			return sh;
		}

		var cmd = Path.Combine(logsDir, safe + ".launch.cmd");
		if (step.shell && string.Equals(step.shellKind, "powershell", StringComparison.OrdinalIgnoreCase))
		{
			// cmd.exe can't run the POSIX/PowerShell script directly — write it to a .launch.ps1 and have the
			// .cmd shim run powershell on it (captures powershell's + the java child's inherited output).
			var ps1 = Path.Combine(logsDir, safe + ".launch.ps1");
			File.WriteAllText(ps1, argsText + "\r\n");
			File.WriteAllText(cmd, $"@powershell -NoProfile -ExecutionPolicy Bypass -File \"{ps1}\"\r\n");
		}
		else
		{
			File.WriteAllText(cmd, inner + "\r\n");
		}

		return cmd;
	}

	/// <summary>Attached mode: streams one of a child's piped readers to the console (prefixed with the step
	/// name), feeds the readiness buffer, and — when <paramref name="teePath"/> is set (--save-logs) — appends
	/// each line to the log file.</summary>
	private static async Task PumpAsync(StreamReader reader, string name, bool isError, StreamLineBuffer buffer,
		string teePath)
	{
		try
		{
			string line;
			while ((line = await reader.ReadLineAsync()) != null)
			{
				if (isError) Log.Warning($"[{name}] {line}");
				else Log.Information($"[{name}] {line}");
				buffer.Append(line);
				if (teePath != null)
				{
					try { File.AppendAllText(teePath, line + Environment.NewLine); }
					catch { /* best-effort tee */ }
				}
			}
		}
		catch { /* reader closes when the child exits or is killed */ }
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

	public static bool IsMvn(string command) =>
		command != null && (command.Equals("mvn", StringComparison.OrdinalIgnoreCase)
		                    || command.Equals("mvn.cmd", StringComparison.OrdinalIgnoreCase));

	/// <summary>Inserts a <c>clean</c> goal before the first build phase in an mvn argument string. Maven runs
	/// goals in command-line order, so <c>clean</c> must precede <c>package</c>/<c>install</c>/etc.</summary>
	public static string InsertCleanBeforeMavenGoal(string args)
	{
		foreach (var goal in new[] { "package", "install", "verify", "test", "compile" })
		{
			var token = " " + goal;
			var idx = args.IndexOf(token, StringComparison.Ordinal);
			if (idx >= 0) return args.Substring(0, idx) + " clean" + args.Substring(idx);
		}
		return "clean " + args;
	}

	/// <summary>Creates/truncates a per-step log file for a fresh run. Opens with shared read/write so the
	/// tailer and the launched child can both hold it; a genuine lock (non-writable dir, or a concurrent
	/// second `up`) surfaces as an actionable <see cref="CliException"/> rather than a raw IOException.</summary>
	private static void ResetLog(string path)
	{
		try
		{
			using var _ = new FileStream(path, FileMode.Create, FileAccess.Write,
				FileShare.ReadWrite | FileShare.Delete);
		}
		catch (IOException e)
		{
			throw new CliException(
				$"Could not reset log '{path}': {e.Message}. It may be held by another process — a previous " +
				"`beam local up` that is still running or was interrupted. Run `beam local stop`, close any other " +
				"`beam local up`, or kill the leftover process, then retry.");
		}
	}

	/// <summary>Single-quotes a value for <c>/bin/sh</c>.</summary>
	private static string Sq(string s) => "'" + (s ?? string.Empty).Replace("'", "'\\''") + "'";

	private static string WinQuote(string s) =>
		string.IsNullOrEmpty(s) ? "\"\"" : (s.Contains(' ') || s.Contains('"') ? "\"" + s.Replace("\"", "\\\"") + "\"" : s);

	private static string ResolveExecutable(string command, string workDir)
	{
		if (string.IsNullOrWhiteSpace(command))
			throw new CliException("A non-beam/non-shell step must define a 'command'.");

		// Prefer the executable inside the working directory when it exists there. This covers an explicit
		// relative path (./BeamableGateway) AND a bare exe name (BeamableGateway.exe): resolving the latter to
		// a full path lets it launch even when cmd.exe won't search the cwd (NoDefaultCurrentDirectoryInExePath).
		// Commands not present in workDir (docker, npm.cmd) fall through and resolve via PATH as before.
		if (!string.IsNullOrEmpty(workDir))
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
			return AwaitCompletion(step, l, config, baseProgress);

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

	private async Task AwaitCompletion(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress)
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

		// A build that exits 0 without producing what it declared is the reference script's `[[ -x $BIN ]] ||
		// die`: the run step after it would launch a binary that isn't there and the stack would come up
		// silently missing a service, so fail here instead.
		var output = LocalStackConfigIO.ResolveRequiredOutput(step, config);
		if (output != null && !File.Exists(output) && !Directory.Exists(output))
		{
			throw new CliException(
				$"Step '{step.name}' completed but did not produce '{output}'. Check the step's command and " +
				"working directory in the manifest (or re-run `beam local init`).");
		}

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
		// Attached: read readiness from the live stream buffer; detached: tail the log files. Same scan either way.
		var sources = l.OpenLineSources();
		try
		{
			var lastLine = "";
			var waited = 0;
			var nextBeat = 10;

			while (true)
			{
				token.ThrowIfCancellationRequested();

				if (waited >= timeout)
				{
					// The gate never tripped. The `readyRetries` path below only fires when the process EXITS, so a
					// service that catches its own startup failure and then parks was never retried — the Scala
					// gateway losing the Mongo connect race is the standing example. The bring-up logged
					// "continuing anyway" and still printed "Stack is up" with nothing listening on 9002, so every
					// `/basic/*` call through Caddy 502'd and every microservice and portal extension died on
					// startup, which reads as "the whole stack is broken" rather than "one step is hung".
					//
					// Only retry when the step is PROVABLY not serving (see <see cref="StepIsDeadOnItsPort"/>).
					// Missing this gate is usually a false negative — on a good run a dozen Scala services report
					// "did not signal ready" while serving traffic fine — so retrying on the timeout alone would
					// kill and relaunch a working stack.
					if (retriesLeft > 0 && relaunch != null && StepIsDeadOnItsPort(step))
					{
						retriesLeft--;
						Log.Warning($"[{step.name}] never signalled ready and nothing is listening on port {step.port} — " +
						            $"hung rather than slow; killing and retrying ({retriesLeft} left). Last log: {LastLogLine(l)}");
						Send(step.name, "starting",
							$"hung with port {step.port} unbound — retrying ({retriesLeft} left)", baseProgress);

						// Kill first: unlike the exit path below, this process is still alive, and leaving it behind
						// orphans a JVM that would race the relaunch for the same port.
						KillLaunched(l);
						try { await Task.Delay(3000, token); }
						catch (OperationCanceledException) { return; }

						l = relaunch();
						foreach (var s in sources) s.Dispose();
						sources = l.OpenLineSources();
						lastLine = "";
						waited = 0;
						nextBeat = 10;
						continue;
					}

					Send(step.name, "running", $"did not signal ready within {timeout}s; continuing", baseProgress);
					Log.Warning($"[{step.name}] did not signal ready within {timeout}s — continuing anyway.");
					return;
				}

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

						// Relaunch and re-watch the fresh source (new stream buffer, or new tailers over the
						// truncated log files).
						l = relaunch();
						foreach (var s in sources) s.Dispose();
						sources = l.OpenLineSources();
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
					foreach (var line in sources.SelectMany(s => s.ReadAvailableLines()))
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
		}
		finally
		{
			foreach (var s in sources) s.Dispose();
		}
	}

	/// <summary>
	/// True when a step that missed its readiness gate is provably not serving: it declares the TCP port it binds
	/// and nothing holds that port. Steps leaving <c>port</c> at 0 always answer false — a missed gate there is
	/// usually a false negative (most Scala services register correctly but never log the exact substring the gate
	/// looks for), and treating that as failure would kill and relaunch healthy services. Only the three steps
	/// whose whole job is to serve a port declare one: the Scala gateway, the C# gateway and the portal frontend.
	/// </summary>
	private static bool StepIsDeadOnItsPort(LocalStackStep step) =>
		step.port > 0 && !LocalStackPortGuard.IsPortTaken(step.port);

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
	// Teardown (attached exit/Ctrl-C, and the detached bring-up failure path)
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
				// Tree-kill plus the command-line sweep for detached grandchildren — see KillLaunched.
				KillLaunched(l);

				Log.Information($"[{l.Step.name}] stopped");
			}
			catch (Exception e)
			{
				Log.Warning($"[{l.Step.name}] failed to stop: {e.Message}");
			}
		}
	}

	private static void KillPid(int pid)
	{
		try { Process.GetProcessById(pid).Kill(entireProcessTree: true); }
		catch { /* already gone */ }
	}

	/// <summary>
	/// Kills one launched step's whole process tree, best-effort. On Windows the tracked pid is usually a shell
	/// wrapper and the runtime grandchild may already have detached from it, so the tree-kill is followed by a
	/// sweep for the step's own command-line token (reusing <c>stop</c>'s per-kind derivation) — otherwise a JVM
	/// outlives its parent and keeps holding the port the caller is about to relaunch onto.
	/// </summary>
	private static void KillLaunched(Launched l)
	{
		try
		{
			if (!l.Process.HasExited)
				l.Process.Kill(entireProcessTree: true);
		}
		catch { /* already gone */ }

		try
		{
			var probe = new LocalStackRunEntry
			{
				name = l.Step.name,
				kind = l.Kind,
				matchToken = l.Step.mainClass,
				workingDirectory = l.WorkingDirectory,
			};
			foreach (var pid in LocalStackProcess.FindByCommandLine(
				         LocalStackStopCommand.BuildKillTokens(probe), LocalStackProcess.ServiceImages))
				KillPid(pid);
		}
		catch { /* best-effort */ }
	}

	private static int SafePid(Launched l)
	{
		try { return l.Process.Id; }
		catch { return 0; }
	}

	/// <summary>
	/// Windows only: rewrites each long-running step's recorded pid from the <c>cmd.exe</c> wrapper to the
	/// real service process (the topmost non-wrapper descendant — the JVM, node, dotnet runner, or gateway
	/// exe), so <c>ps</c>/<c>stop</c> and the next <c>up</c>'s idempotency check track the process that
	/// actually survives (and whose tree-kill takes the whole step down) rather than a wrapper that exits.
	/// No-op on unix (the launcher <c>exec</c>s the service, so the tracked pid already is the service) and
	/// for run-to-completion (docker) steps.
	/// </summary>
	private void ReconcileLeafPids(LocalStackRunState runState, string runStatePath, List<Launched> launched)
	{
		if (!OperatingSystem.IsWindows())
			return;

		var changed = false;
		lock (_launchedLock)
		{
			// ONLY the steps this run launched. A carried-over entry's pid is already the resolved service leaf, and
			// ResolveServiceRootPid always descends to a child — so re-reconciling it would walk the recorded pid
			// one level deeper on every `up`, until `ps`/`stop` were pointing at a helper process (esbuild, a
			// dotnet child) while the real service kept running and holding its port.
			var launchedNames = new HashSet<string>(launched.Select(l => l.Step.name), StringComparer.OrdinalIgnoreCase);

			foreach (var entry in runState.steps)
			{
				if (entry.waitForExit || entry.pid <= 0 || !launchedNames.Contains(entry.name))
					continue;

				var service = LocalStackProcess.ResolveServiceRootPid(entry.pid);
				if (service != entry.pid && service > 0)
				{
					Log.Verbose($"[{entry.name}] tracking service pid={service} (was wrapper pid={entry.pid})");
					entry.pid = service;
					// Re-pin: the start time recorded at launch belongs to the wrapper, not to this leaf.
					entry.startedAtUtcTicks = LocalStackLiveness.StartTicksOf(service);
					changed = true;
				}
			}

			if (changed)
				LocalStackRunStateIO.Save(runStatePath, runState);
		}
	}
}
