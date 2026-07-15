using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Server;

namespace cli.Services.Sandbox;

/// <summary>
/// Out-of-process invocation runner. Re-executes the sandbox's own CLI binary
/// (the same <c>beam</c> the sandbox is running as) as a child process and pipes
/// its stdout / stderr into the invocation sink line-by-line.
///
/// <para>This exists because in-process invocation can't host commands that
/// stand up their own <c>HttpListener</c> — the sandbox is itself a hosted
/// microservice already holding the listener prefix, and EmbedIO can't share.
/// <c>project run</c> in particular fails with "Prefix already in use" if run
/// in-process while the sandbox is alive. A subprocess gets its own listener
/// namespace, its own static state, and crash isolation as a bonus.</para>
///
/// <para>Lifecycle: cancellation kills the child (and its descendants). For
/// long-running CLI commands that spawn user services (e.g. <c>project run</c>)
/// we auto-append <c>--require-process-id &lt;sandbox-pid&gt;</c> so the spawned
/// user microservice watches the sandbox and self-terminates if the sandbox
/// dies — preventing orphan processes after a sandbox restart.</para>
/// </summary>
public sealed class SubprocessInvocationRunner : IInvocationRunner
{
	// Commands whose long-lived spawned children should be lifetime-tied to
	// the sandbox process via --require-process-id. The CLI's RunProjectCommand
	// already supports this flag — we just pass it through automatically.
	private static readonly string[] PidGuardCommandPrefixes =
	{
		"project run",
		"project start", // future-proof: alias if/when one is added
	};

	private readonly string _executablePath;
	private readonly string _workingDirectory;
	private readonly int _hostProcessId;

	public SubprocessInvocationRunner(string executablePath, string workingDirectory, int hostProcessId)
	{
		_executablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
		_workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
		_hostProcessId = hostProcessId;
	}

	public async Task RunAsync(Invocation invocation, IInvocationSink sink, CancellationToken token)
	{
		var commandLine = invocation.CommandLine ?? string.Empty;
		commandLine = MaybeInjectPidGuard(commandLine);

		var psi = new ProcessStartInfo
		{
			FileName = _executablePath,
			Arguments = commandLine,
			WorkingDirectory = _workingDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		Process process;
		try
		{
			process = Process.Start(psi)
				?? throw new InvalidOperationException("Process.Start returned null");
		}
		catch (Exception ex)
		{
			sink.EmitOutput("error", $"failed to spawn subprocess: {ex.Message}");
			sink.EmitStatus(InvocationStatusKind.Failed, failureReason: ex.Message);
			return;
		}

		SandboxLog.Info($"[sandbox-subprocess] spawned pid={process.Id} cmd=[{commandLine}]");

		// Stream consumers — stdout is the structured-JSON channel commands use
		// via IDataReporterService; stderr is raw. We tag them so Portal can
		// colour them differently without parsing.
		var stdoutTask = PumpAsync(process.StandardOutput, "stream", sink, token);
		var stderrTask = PumpAsync(process.StandardError, "error", sink, token);

		// Cancel = kill the whole tree. The child might itself have spawned a
		// dotnet build / dotnet run; entireProcessTree:true gets those too.
		using var cancelRegistration = token.Register(() =>
		{
			try
			{
				if (!process.HasExited)
				{
					SandboxLog.Info($"[sandbox-subprocess] cancel → killing pid={process.Id}");
					process.Kill(entireProcessTree: true);
				}
			}
			catch (Exception ex)
			{
				SandboxLog.Warn($"[sandbox-subprocess] kill failed pid={process.Id}: {ex.Message}");
			}
		});

		try
		{
			await process.WaitForExitAsync(token);
		}
		catch (OperationCanceledException)
		{
			// Process.Kill above; drain remaining pipes below so trailing output isn't lost.
		}

		// Drain residual pipe contents whether we exited naturally or were killed.
		await Task.WhenAll(stdoutTask, stderrTask);

		if (token.IsCancellationRequested)
		{
			sink.EmitStatus(InvocationStatusKind.Cancelled);
			return;
		}

		var exitCode = process.ExitCode;
		var status = exitCode == 0 ? InvocationStatusKind.Completed : InvocationStatusKind.Failed;
		sink.EmitStatus(status, exitCode);
	}

	private string MaybeInjectPidGuard(string commandLine)
	{
		var trimmed = commandLine.TrimStart();
		var matchesGuard = PidGuardCommandPrefixes.Any(prefix =>
			trimmed.StartsWith(prefix, StringComparison.Ordinal) &&
			(trimmed.Length == prefix.Length || char.IsWhiteSpace(trimmed[prefix.Length])));
		if (!matchesGuard) return commandLine;

		// If the caller already passed their own pid guard, respect it.
		if (commandLine.Contains("--require-process-id", StringComparison.Ordinal))
			return commandLine;

		return $"{commandLine} --require-process-id {_hostProcessId}";
	}

	private static async Task PumpAsync(StreamReader reader, string channel, IInvocationSink sink, CancellationToken token)
	{
		try
		{
			while (true)
			{
				var line = await reader.ReadLineAsync(token);
				if (line == null) return;
				sink.EmitOutput(channel, line);
			}
		}
		catch (OperationCanceledException) { /* draining cancelled — fine */ }
		catch (IOException) { /* pipe closed mid-read — fine */ }
		catch (Exception ex)
		{
			SandboxLog.Warn($"[sandbox-subprocess] pump error channel={channel}: {ex.Message}");
		}
	}
}
