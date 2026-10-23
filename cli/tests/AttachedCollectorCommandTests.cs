using cli;
using cli.OtelCommands;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace tests;

public class AttachedCollectorCommandTests
{
	internal static readonly Func<int, CancellationToken, Task> WaitForCollectorExit =
		typeof(StartCollectorCommand).GetMethod("WaitForCollectorExit", BindingFlags.NonPublic | BindingFlags.Static)!
			.CreateDelegate<Func<int, CancellationToken, Task>>();

	[Test]
	public async Task Attached_wait_reports_collector_exit()
	{
		await using var child = await HeldChild.Start();
		var wait = WaitForCollectorExit(child.Id, CancellationToken.None);
		Assert.That(wait.IsCompleted, Is.False, "A live collector must keep the attached command running.");
		await child.Exit();
		var error = Assert.ThrowsAsync<CliException>(async () => await wait.WaitAsync(TimeSpan.FromSeconds(3)));
		Assert.That(error!.Message, Does.Contain(child.Id.ToString()).And.Contain("exited"));
	}

	[Test]
	public async Task Attached_wait_reports_a_collector_that_already_exited()
	{
		int pid;
		await using (var child = await HeldChild.Start())
		{
			pid = child.Id;
			await child.Exit();
		}
		// Dispose the last process handle before looking the PID up again.
		var error = Assert.ThrowsAsync<CliException>(async () =>
			await WaitForCollectorExit(pid, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3)));
		Assert.That(error!.Message, Does.Contain(pid.ToString()).And.Contain("exited"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Attached_wait_honors_cancellation_without_stopping_the_collector(bool alreadyCancelled)
	{
		await using var child = await HeldChild.Start();
		using var cancellation = new CancellationTokenSource();
		if (alreadyCancelled) cancellation.Cancel();
		var wait = WaitForCollectorExit(child.Id, cancellation.Token);
		if (!alreadyCancelled)
		{
			Assert.That(wait.IsCompleted, Is.False);
			cancellation.Cancel();
			cancellation.Cancel();
		}
		Assert.CatchAsync<OperationCanceledException>(async () => await wait.WaitAsync(TimeSpan.FromSeconds(3)));
		Assert.That(child.HasExited, Is.False, "Cancelling a wait must not kill a collector shared by other commands.");
	}

	// A disposable child held on stdin, with no dependency on a real collector or its discovery port.
	internal sealed class HeldChild : IAsyncDisposable
	{
		private readonly Process _process;
		public int Id => _process.Id;
		public bool HasExited => _process.HasExited;
		private HeldChild(Process process) => _process = process;
		public static async Task<HeldChild> Start()
		{
			var start = new ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh")
			{
				UseShellExecute = false, CreateNoWindow = true,
				RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
			};
			if (OperatingSystem.IsWindows())
			{
				start.ArgumentList.Add("/d");
				start.ArgumentList.Add("/c");
				start.ArgumentList.Add("echo ready& set /p beam_test_input=");
			}
			else
			{
				start.ArgumentList.Add("-c");
				start.ArgumentList.Add("printf 'ready\\n'; read beam_test_input");
			}
			var child = new HeldChild(Process.Start(start)!);
			try
			{
				Assert.That(await child._process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("ready"));
				return child;
			}
			catch { await child.DisposeAsync(); throw; }
		}
		public async Task Exit()
		{
			await _process.StandardInput.WriteLineAsync("exit");
			await _process.StandardInput.FlushAsync();
			await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
		}
		public async ValueTask DisposeAsync()
		{
			if (!_process.HasExited)
			{
				_process.Kill(true);
				await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
			}
			_process.Dispose();
		}
	}
}
