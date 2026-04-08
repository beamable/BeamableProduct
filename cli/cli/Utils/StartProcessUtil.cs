using microservice.Extensions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace cli.Utils;

public class StartProcessResult
{
	public int exit;
	public string stdout;
	public string stderr;
}

/// <summary>
/// Holds a reference to a started <see cref="Process"/> and a <see cref="Task"/> that completes when the process exits.
/// Callers can wire up their own cancellation / progress logic against these two handles,
/// or call <see cref="WaitForResult"/> to block until exit and collect stdout/stderr.
/// </summary>
public class ProcessHandle
{
	/// <summary>
	/// The running process.
	/// </summary>
	public Process Process { get; init; }

	/// <summary>
	/// A task that completes when the process raises its <see cref="Process.Exited"/> event.
	/// </summary>
	public Task ExitedTask { get; init; }

	internal StringBuilder _stdout;
	internal StringBuilder _stderr;

	/// <summary>
	/// Blocks until the process exits and returns a <see cref="StartProcessResult"/>
	/// with the captured exit code, stdout, and stderr.
	/// </summary>
	public StartProcessResult WaitForResult()
	{
		Process.WaitForExit();
		return new StartProcessResult
		{
			exit = Process.ExitCode,
			stdout = _stdout?.ToString() ?? string.Empty,
			stderr = _stderr?.ToString() ?? string.Empty
		};
	}
}

public static class StartProcessUtil
{
	/// <summary>
	/// Starts a process with redirected stdout / stderr.
	/// When <paramref name="isDetach"/> is <c>true</c>, the command is automatically wrapped
	/// for OS-specific detached execution (<c>cmd.exe /C</c> on Windows, <c>nohup</c> on Linux/macOS)
	/// so the child process survives parent exit.
	/// When <paramref name="useShell"/> is <c>true</c>, the command is wrapped through the
	/// platform shell (<c>cmd.exe /c</c> on Windows) for PATH resolution.
	/// <para>
	/// Returns a <see cref="ProcessHandle"/>. For async usage, await
	/// <see cref="ProcessHandle.ExitedTask"/> or interact with
	/// <see cref="ProcessHandle.Process"/> directly. For synchronous usage,
	/// call <see cref="ProcessHandle.WaitForResult"/>.
	/// </para>
	/// </summary>
	/// <param name="fileName">Executable path or name.</param>
	/// <param name="args">Command-line arguments.</param>
	/// <param name="isDetach">When true, wraps the command so the child process outlives the parent.</param>
	/// <param name="useShell">When true, wraps the command through the platform shell for PATH resolution.</param>
	/// <param name="environmentVariables">Optional extra environment variables to set on the child process.</param>
	/// <param name="onStdout">Called for every stdout line received (may be null).</param>
	/// <param name="onStderr">Called for every stderr line received (may be null).</param>
	/// <param name="workingDirectoryPath">Optional working directory.</param>
	/// <returns>A handle containing the <see cref="Process"/> and a task that completes on exit.</returns>
	public static ProcessHandle Run(
		string fileName,
		string args,
		bool isDetach = false,
		bool useShell = false,
		Dictionary<string, string> environmentVariables = null,
		Action<string> onStdout = null,
		Action<string> onStderr = null,
		string workingDirectoryPath = null)
	{
		if (useShell && !isDetach)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				args = $"/c {fileName} {args}";
				fileName = "cmd.exe";
			}
		}

		if (isDetach)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				args = "/C " + $"{fileName.EnquotePath()} {args}".EnquotePath('(', ')');
				fileName = "cmd.exe";
			}
			else
			{
				args = $"sh -c \"{fileName} {args}\" &";
				fileName = "nohup";
			}
		}

		var psi = new ProcessStartInfo(fileName, args)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		if (!string.IsNullOrEmpty(workingDirectoryPath))
		{
			psi.WorkingDirectory = workingDirectoryPath;
		}

		if (environmentVariables != null)
		{
			foreach (var (key, value) in environmentVariables)
			{
				psi.Environment[key] = value;
			}
		}

		var proc = Process.Start(psi);
		var exitTcs = new TaskCompletionSource();
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();

		proc.EnableRaisingEvents = true;
		proc.Exited += (_, _) => exitTcs.TrySetResult();

		proc.OutputDataReceived += (_, e) =>
		{
			if (e.Data == null)
			{
				return;
			}

			stdout.AppendLine(e.Data);
			onStdout?.Invoke(e.Data);
		};
		proc.ErrorDataReceived += (_, e) =>
		{
			if (e.Data == null)
			{
				return;
			}

			stderr.AppendLine(e.Data);
			onStderr?.Invoke(e.Data);
		};

		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();

		return new ProcessHandle
		{
			Process = proc,
			ExitedTask = exitTcs.Task,
			_stdout = stdout,
			_stderr = stderr
		};
	}
}
