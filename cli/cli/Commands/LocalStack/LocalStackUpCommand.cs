using Beamable.Server;
using cli.Services.LocalStack;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace cli.Commands.LocalStack;

public class LocalStackUpCommandArgs : CommandArgs
{
	public string configPath;
	public string host;
	public string portalUrl;
	public string only;
	public string skip;
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
/// Foreground orchestrator: brings up every enabled step in the manifest in order, waits for each
/// readiness gate, streams progress, then keeps running (tailing child logs) until cancelled —
/// at which point every process this command started is torn down.
/// </summary>
public class LocalStackUpCommand
	: StreamCommand<LocalStackUpCommandArgs, LocalStackUpResultStream>
	, IStandaloneCommand, ISkipManifest
{
	private readonly object _launchedLock = new object();
	private int _tornDown;

	public LocalStackUpCommand() : base("up", "Bring up the local stack from the manifest and tail it until cancelled")
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
	}

	private static HashSet<string> NameSet(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

	public override async Task Handle(LocalStackUpCommandArgs args)
	{
		var path = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		if (!File.Exists(path))
			throw new CliException($"No local-stack manifest at {path}. Run `beam local init` to create one.");

		var config = LocalStackConfigIO.Load(path);
		if (!string.IsNullOrWhiteSpace(args.host)) config.host = args.host;
		if (!string.IsNullOrWhiteSpace(args.portalUrl)) config.portalUrl = args.portalUrl;

		var only = NameSet(args.only);
		var skip = NameSet(args.skip);

		bool Included(LocalStackStep s) =>
			s.enabled
			&& (only == null || only.Contains(s.name))
			&& (skip == null || !skip.Contains(s.name));

		var steps = config.steps.Where(Included).ToList();
		if (steps.Count == 0)
			throw new CliException("No steps to run (all disabled or filtered out).");

		// Resolve how to invoke this same beam CLI for `beam: true` steps, and the workspace to run
		// them from (so they can see the local service manifest) when a step declares no working dir.
		var (beamExe, beamLeading) = ResolveBeam(args);
		var beamWorkspaceFallback = args.ConfigService?.BeamableWorkspace
		                            ?? args.ConfigService?.WorkingDirectory
		                            ?? Directory.GetCurrentDirectory();

		var launched = new List<Launched>();
		var token = args.Lifecycle.CancellationToken;

		// Tear down on cancellation. The CLI's Ctrl+C handler calls lifecycle.Cancel() and then
		// hard-exits (Environment.Exit) shortly after — which skips finally blocks. Registered
		// token callbacks run synchronously inside Cancel(), before that hard exit, so this is what
		// actually guarantees the children are stopped. The finally below is a fallback.
		token.Register(() => TearDown(launched));

		// Also handle POSIX termination signals directly. The framework only wires SIGINT (Ctrl+C);
		// closing the terminal (SIGHUP) or a `kill` (SIGTERM) would otherwise orphan the children.
		// We cancel the signal's default action, stop the children, then cancel the lifecycle so the
		// command returns cleanly.
		var signalRegs = new List<IDisposable>();
		if (!OperatingSystem.IsWindows())
		{
			void OnSignal(PosixSignalContext ctx)
			{
				ctx.Cancel = true;
				try { TearDown(launched); } catch { /* never let the signal handler throw */ }
				try { args.Lifecycle.Cancel(); } catch { /* ignore */ }
			}

			foreach (var sig in new[] { PosixSignal.SIGINT, PosixSignal.SIGTERM, PosixSignal.SIGQUIT, PosixSignal.SIGHUP })
			{
				try { signalRegs.Add(PosixSignalRegistration.Create(sig, OnSignal)); }
				catch { /* signal not supported on this platform */ }
			}
		}

		try
		{
			var i = 0;
			while (i < steps.Count)
			{
				token.ThrowIfCancellationRequested();

				// Gather the next batch: a run of consecutive steps sharing a non-empty group is
				// launched and awaited in parallel; an ungrouped step is a batch of one (sequential).
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
					Send(step.name, "starting", $"launching ({idx + 1}/{steps.Count})", baseProgress);

					var l = StartStep(step, config, beamExe, beamLeading, beamWorkspaceFallback);
					lock (_launchedLock) launched.Add(l);
					Log.Information($"[{step.name}] started (pid={l.Handle.Process.Id})");

					awaits.Add(AwaitStep(step, l, config, baseProgress, steps.Count, token));
				}

				await Task.WhenAll(awaits);
			}

			Send(string.Empty, "running",
				$"Stack is up. Backend={config.host} Portal={config.portalUrl}. Press Ctrl+C to stop everything.", 1f);
			Log.Information($"Stack is up. Backend={config.host}  Portal={config.portalUrl}");
			Log.Information("Tailing child processes — press Ctrl+C to tear everything down.");

			// Keep running (children stream their logs) until cancellation.
			try
			{
				await Task.Delay(Timeout.Infinite, token);
			}
			catch (OperationCanceledException)
			{
				// expected on Ctrl+C
			}
		}
		finally
		{
			foreach (var reg in signalRegs)
			{
				try { reg.Dispose(); } catch { /* ignore */ }
			}

			TearDown(launched);
		}
	}

	private void Send(string step, string status, string message, float progress) =>
		SendResults(new LocalStackUpResultStream
		{
			step = step, status = status, message = message, progressRatio = Math.Clamp(progress, 0f, 1f)
		});

	// ----------------------------------------------------------------------------------
	// Launching
	// ----------------------------------------------------------------------------------

	private class Launched
	{
		public LocalStackStep Step;
		public ProcessRun Handle;
		public volatile bool ReadyLogSeen;
		public volatile string LastLogLine = "";
	}

	private class ProcessRun
	{
		public Process Process;
		public Task ExitedTask;
	}

	private (string exe, string[] leading) ResolveBeam(CommandArgs args)
	{
		// Run the SAME beam build as a subprocess. When hosted by `dotnet` (dev: `dotnet Beamable.Tools.dll`),
		// invoke that exact dll directly (`dotnet <dll> ...`) rather than `dotnet beam` — the latter resolves
		// through the dotnet-tool cache and the child's working directory (local vs global tool), which can pick
		// a stale/absent tool or fail entirely. Running the dll is build- and cwd-independent, matching the
		// original run-local-stack.sh (`dotnet "$BEAM_DLL" ...`). When we're the apphost exe, we already ARE beam.
		var processPath = Environment.ProcessPath ?? string.Empty;
		var isDotnetHost = processPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
		                   || processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);

		if (!isDotnetHost)
			return (processPath, Array.Empty<string>());

		var entryDll = Assembly.GetEntryAssembly()?.Location;
		if (!string.IsNullOrEmpty(entryDll) && File.Exists(entryDll))
			return (processPath, new[] { entryDll });

		// Fallback (e.g. single-file publish with no on-disk dll): the installed `beam` tool.
		return (args.AppContext.DotnetPath, new[] { "beam" });
	}

	private Launched StartStep(LocalStackStep step, LocalStackConfig config, string beamExe, string[] beamLeading,
		string beamWorkspaceFallback)
	{
		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		var argsText = LocalStackConfigIO.Substitute(step.arguments, config) ?? string.Empty;

		// beam sub-commands need to run inside a .beamable workspace to see the local service manifest.
		// If the step didn't declare a (valid) working directory, fall back to this beam's workspace.
		if (step.beam && (string.IsNullOrEmpty(workDir) || !Directory.Exists(workDir)))
			workDir = beamWorkspaceFallback;

		var psi = new ProcessStartInfo
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
		};

		if (step.beam)
		{
			psi.FileName = beamExe;
			foreach (var a in beamLeading) psi.ArgumentList.Add(a);
			foreach (var a in SplitArgs(argsText)) psi.ArgumentList.Add(a);
		}
		else if (step.shell)
		{
			if (OperatingSystem.IsWindows())
			{
				psi.FileName = "cmd.exe";
				psi.ArgumentList.Add("/c");
				psi.ArgumentList.Add(argsText);
			}
			else
			{
				psi.FileName = "/bin/sh";
				psi.ArgumentList.Add("-c");
				psi.ArgumentList.Add(argsText);
			}
		}
		else
		{
			psi.FileName = ResolveExecutable(step.command, workDir);
			foreach (var a in SplitArgs(argsText)) psi.ArgumentList.Add(a);
		}

		if (!string.IsNullOrEmpty(workDir) && Directory.Exists(workDir))
			psi.WorkingDirectory = workDir;

		foreach (var (k, v) in step.environment)
			psi.Environment[k] = LocalStackConfigIO.Substitute(v, config);

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

		proc.StandardInput.Close();

		var launched = new Launched { Step = step };
		var exitTcs = new TaskCompletionSource();
		proc.EnableRaisingEvents = true;
		proc.Exited += (_, _) => exitTcs.TrySetResult();

		void OnLine(string line, bool isError)
		{
			if (line == null) return;
			launched.LastLogLine = line;
			if (!string.IsNullOrEmpty(step.readyWhenLogContains) && line.Contains(step.readyWhenLogContains))
				launched.ReadyLogSeen = true;
			if (isError) Log.Warning($"[{step.name}] {line}");
			else Log.Information($"[{step.name}] {line}");
		}

		proc.OutputDataReceived += (_, e) => OnLine(e.Data, false);
		proc.ErrorDataReceived += (_, e) => OnLine(e.Data, true);
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();

		launched.Handle = new ProcessRun { Process = proc, ExitedTask = exitTcs.Task };
		return launched;
	}

	private static string ResolveExecutable(string command, string workDir)
	{
		if (string.IsNullOrWhiteSpace(command))
			throw new CliException("A non-beam/non-shell step must define a 'command'.");

		// .NET resolves a relative FileName against the CURRENT process cwd, not WorkingDirectory,
		// so resolve command paths that are relative to the step's working directory ourselves.
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

	/// <summary>Applies a single step's gate: wait-for-exit, readiness poll, or nothing.</summary>
	private Task AwaitStep(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress, int totalSteps,
		CancellationToken token)
	{
		if (step.waitForExit)
			return AwaitCompletion(step, l, baseProgress);

		if (!string.IsNullOrEmpty(step.readyWhenHttpOk) || !string.IsNullOrEmpty(step.readyWhenLogContains))
			return AwaitReadiness(step, l, config, baseProgress, token);

		Send(step.name, "running", "no readiness gate; assuming up", baseProgress + 1f / totalSteps);
		return Task.CompletedTask;
	}

	private async Task AwaitCompletion(LocalStackStep step, Launched l, float baseProgress)
	{
		Send(step.name, "starting", "waiting for completion", baseProgress);
		var timeout = TimeSpan.FromSeconds(Math.Max(1, step.readyTimeoutSeconds));
		var done = await Task.WhenAny(l.Handle.ExitedTask, Task.Delay(timeout));
		if (done != l.Handle.ExitedTask)
		{
			Send(step.name, "failed", $"did not complete within {step.readyTimeoutSeconds}s", baseProgress);
			Log.Warning($"[{step.name}] did not complete within {step.readyTimeoutSeconds}s — continuing.");
			return;
		}

		var code = SafeExitCode(l);
		if (code != 0)
			throw new CliException($"Step '{step.name}' exited with code {code}. Last log: {l.LastLogLine}");
		Send(step.name, "ready", "completed", baseProgress);
	}

	private async Task AwaitReadiness(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress,
		CancellationToken token)
	{
		var url = LocalStackConfigIO.Substitute(step.readyWhenHttpOk, config);
		var timeout = Math.Max(1, step.readyTimeoutSeconds);
		Send(step.name, "starting", $"waiting for readiness (timeout {timeout}s)", baseProgress);

		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
		var waited = 0;
		var nextBeat = 10;

		while (waited < timeout)
		{
			token.ThrowIfCancellationRequested();

			if (l.Handle.ExitedTask.IsCompleted)
			{
				var code = SafeExitCode(l);
				Send(step.name, "failed", $"process exited early (code {code})", baseProgress);
				Log.Warning($"[{step.name}] exited before becoming ready (code {code}). Last log: {l.LastLogLine}");
				return;
			}

			if (l.ReadyLogSeen)
			{
				Send(step.name, "ready", $"ready after {waited}s", baseProgress);
				Log.Information($"[{step.name}] ready after {waited}s.");
				return;
			}

			if (!string.IsNullOrEmpty(url) && await HttpOk(http, url, token))
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
				var hint = string.IsNullOrEmpty(l.LastLogLine) ? "" : $" | {Trim(l.LastLogLine, 110)}";
				Send(step.name, "starting", $"still starting — {waited}/{timeout}s{hint}", baseProgress);
				Log.Information($"[{step.name}] still starting — {waited}/{timeout}s{hint}");
			}
		}

		Send(step.name, "running", $"did not signal ready within {timeout}s; continuing", baseProgress);
		Log.Warning($"[{step.name}] did not signal ready within {timeout}s — continuing anyway.");
	}

	private static async Task<bool> HttpOk(HttpClient http, string url, CancellationToken token)
	{
		try
		{
			using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
			return true; // any HTTP response means the port is serving
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
		try { return l.Handle.Process.HasExited ? l.Handle.Process.ExitCode : 0; }
		catch { return 0; }
	}

	private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

	// ----------------------------------------------------------------------------------
	// Teardown
	// ----------------------------------------------------------------------------------

	private void TearDown(List<Launched> launched)
	{
		// Idempotent: may be invoked both by the cancellation-token callback and the finally block.
		if (Interlocked.Exchange(ref _tornDown, 1) == 1) return;

		List<Launched> snapshot;
		lock (_launchedLock) snapshot = launched.ToList();
		if (snapshot.Count == 0) return;

		// This can run on a POSIX signal-handler thread where the logger/reporter are not safe to
		// touch. Killing the child processes is the critical action, so do it FIRST and never let a
		// logging failure abort it. All logging/streaming below is strictly best-effort.
		SafeSend(string.Empty, "tearing-down", "stopping child processes", 1f);

		// Stop in reverse start order.
		for (var i = snapshot.Count - 1; i >= 0; i--)
		{
			var l = snapshot[i];
			try
			{
				if (l.Handle.Process.HasExited) continue;
				l.Handle.Process.Kill(entireProcessTree: true);
				SafeLog($"[{l.Step.name}] stopped");
			}
			catch (Exception e)
			{
				SafeLog($"[{l.Step.name}] failed to stop: {e.Message}");
			}
		}

		SafeSend(string.Empty, "stopped", "local stack stopped", 1f);
	}

	private void SafeLog(string message)
	{
		try { Log.Information(message); } catch { /* logger may be unavailable on a signal thread */ }
	}

	private void SafeSend(string step, string status, string message, float progress)
	{
		try { Send(step, status, message, progress); } catch { /* reporter may be unavailable on a signal thread */ }
	}
}
