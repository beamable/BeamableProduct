using System;
using System.Diagnostics;
using System.Linq;
using cli.Services.LocalStack;
using NUnit.Framework;
// LocalStackProcess + LocalStackLiveness both live in cli.Services.LocalStack.

namespace tests;

/// <summary>
/// Covers the liveness check `up` / `ps` / `stop` use on a previously recorded step.
///
/// This exists because a live pid alone is not evidence: pids get recycled. Observed on a real machine — a
/// recorded `scala: auth` pid had been recycled by an unrelated audio service, so `up` announced
/// "already running (pid=6344) — skipping" and brought the whole stack up with no auth service, `ps` agreed it
/// was running, and `stop` would have killed the bystander. Pinning the pid to its start time makes the pid
/// identify one specific process.
/// </summary>
public class LocalStackLivenessTests
{
	private static LocalStackRunEntry EntryFor(Process process) => new LocalStackRunEntry
	{
		name = "scala: auth",
		kind = "shell",
		pid = process.Id,
		startedAtUtcTicks = LocalStackLiveness.StartTicksOf(process.Id),
	};

	private static bool IsRunning(LocalStackRunEntry entry) =>
		LocalStackLiveness.IsEntryRunning(entry, Array.Empty<string>());

	private static LocalStackLiveness.Liveness Check(LocalStackRunEntry entry, params string[] tokens) =>
		LocalStackLiveness.Check(entry, tokens);

	/// <summary>
	/// The legacy (no recorded start time) branch, driven directly so every outcome is covered rather than
	/// depending on what the test host happens to be called.
	///
	/// The critical row is the Unverified one: promoting "could not verify" to "running" is how a recycled pid was
	/// treated as a live service (and how `stop` would have tree-killed a bystander), while promoting it to
	/// "stopped" would relaunch a service that is actually up. Each caller picks its safe side, so this must not.
	/// </summary>
	[TestCase("SenaryAudioApp.Svc", "C:/audio/app.exe", "tools/auth/", ExpectedResult = LocalStackLiveness.Liveness.Stopped,
		TestName = "unrelated image on a recycled pid => Stopped")]
	[TestCase("java", "java -cp tools/auth/target/classes com.disruptorbeam.auth.App", "tools/auth/",
		ExpectedResult = LocalStackLiveness.Liveness.Running, TestName = "our JVM carrying the step token => Running")]
	[TestCase("java", "java -cp tools/stats/target/classes com.disruptorbeam.stats.App", "tools/auth/",
		ExpectedResult = LocalStackLiveness.Liveness.Stopped, TestName = "a DIFFERENT service's JVM => Stopped")]
	[TestCase("java", null, "tools/auth/", ExpectedResult = LocalStackLiveness.Liveness.Unverified,
		TestName = "plausible image but unreadable command line => Unverified")]
	[TestCase("java", "java -cp whatever", null, ExpectedResult = LocalStackLiveness.Liveness.Unverified,
		TestName = "plausible image but no usable identity token => Unverified")]
	[TestCase("dotnet", "dotnet Beamable.Tools.dll project run --ids CampaignService", "CampaignService",
		ExpectedResult = LocalStackLiveness.Liveness.Running, TestName = "beam step matched on its service id")]
	[TestCase("dotnet", "dotnet build SomeUnrelated.csproj", "CampaignService",
		ExpectedResult = LocalStackLiveness.Liveness.Stopped, TestName = "unrelated dotnet on a recycled pid => Stopped")]
	public LocalStackLiveness.Liveness Legacy_classification(string image, string commandLine, string token) =>
		LocalStackLiveness.ClassifyLegacy(image, commandLine, token == null ? null : new[] { token });

	/// <summary>
	/// The single-pid command-line read is what makes the legacy branch verifiable off Windows at all — without it
	/// every legacy entry on macOS/Linux stayed Unverified, which is what let any recycled java/node/dotnet pid
	/// pass as a live service.
	/// </summary>
	[Test]
	public void A_pids_own_command_line_is_readable_and_carries_its_arguments()
	{
		var self = Process.GetCurrentProcess();
		var commandLine = LocalStackProcess.TryGetCommandLine(self.Id);

		Assert.That(commandLine, Is.Not.Null.And.Not.Empty,
			"reading one pid's command line must work on this platform");
		// The test host is always launched with its own dll on the command line, so this is a real identity token.
		Assert.That(commandLine, Does.Contain("test").IgnoreCase.Or.Contain("dotnet").IgnoreCase);
	}

	[Test]
	public void Reading_one_pids_command_line_works_on_this_platform()
	{
		// The cross-platform single-pid lookup is what makes legacy entries verifiable off Windows.
		Assert.That(LocalStackProcess.TryGetCommandLine(Process.GetCurrentProcess().Id),
			Is.Not.Null.And.Not.Empty);
		Assert.That(LocalStackProcess.TryGetCommandLine(0), Is.Null);
	}

	[Test]
	public void A_recorded_process_that_is_still_running_reads_as_running()
	{
		var entry = EntryFor(Process.GetCurrentProcess());

		Assert.That(entry.startedAtUtcTicks, Is.GreaterThan(0), "the start time must be recordable");
		Assert.That(IsRunning(entry), Is.True);
	}

	/// <summary>
	/// The regression guard. Same live pid, different start time — i.e. the number was recycled by another
	/// process — must NOT read as running. Before this, `up` skipped launching the service.
	/// </summary>
	[Test]
	public void A_recycled_pid_does_not_read_as_running()
	{
		var entry = EntryFor(Process.GetCurrentProcess());
		entry.startedAtUtcTicks -= TimeSpan.FromMinutes(10).Ticks; // recorded process started well before this one

		Assert.That(IsRunning(entry), Is.False,
			"a live pid whose start time doesn't match the recorded one is a different process");
	}

	[Test]
	public void Small_start_time_differences_are_tolerated()
	{
		var entry = EntryFor(Process.GetCurrentProcess());
		entry.startedAtUtcTicks -= TimeSpan.FromSeconds(2).Ticks; // clock/rounding slack, not a different process

		Assert.That(IsRunning(entry), Is.True);
	}

	[Test]
	public void A_dead_pid_does_not_read_as_running()
	{
		var proc = Process.Start(new ProcessStartInfo
		{
			FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
			Arguments = OperatingSystem.IsWindows() ? "/c exit 0" : "-c 'exit 0'",
			UseShellExecute = false,
			CreateNoWindow = true,
		});
		Assert.That(proc, Is.Not.Null);

		var entry = EntryFor(proc);
		proc.WaitForExit();
		var pid = proc.Id;
		proc.Dispose();

		Assert.That(IsRunning(entry), Is.False, $"pid {pid} has exited");
	}

	[Test]
	public void Run_to_completion_steps_are_never_judged_by_their_pid()
	{
		// A docker step's pid is EXPECTED to be dead; liveness must not claim it is up just because the pid
		// number happens to be alive again.
		var entry = EntryFor(Process.GetCurrentProcess());
		entry.name = "docker: api deps + caddy";
		entry.waitForExit = true;

		Assert.That(IsRunning(entry), Is.False);
	}

	[Test]
	public void An_unset_pid_reads_as_not_running()
	{
		Assert.That(IsRunning(new LocalStackRunEntry { name = "scala: auth", pid = 0 }), Is.False);
		Assert.That(LocalStackLiveness.StartTicksOf(0), Is.EqualTo(0));
	}

	/// <summary>
	/// End-to-end over a real process: a legacy entry (no recorded start time) whose pid is now held by something
	/// that is not a stack runtime must read as Stopped — the exact shape of the bug that was hit, where an audio
	/// service had inherited a dead `scala: auth` pid.
	/// </summary>
	[Test]
	public void A_legacy_entry_rejects_a_pid_held_by_a_non_stack_process()
	{
		var current = Process.GetCurrentProcess().ProcessName;
		var stackImages = new[]
		{
			"java", "javaw", "node", "dotnet", "beam", "Beamable.Tools",
			"BeamableGateway", "BeamableMessageRailRuntime", "BeamableCampaignRuntime"
		};
		if (stackImages.Contains(current, StringComparer.OrdinalIgnoreCase))
		{
			// The pure-function cases above still cover every branch; only this live-process variant needs a host
			// that isn't itself a stack image.
			Assert.Ignore($"test host runs as '{current}', which is itself a stack image");
		}

		var legacy = EntryFor(Process.GetCurrentProcess());
		legacy.startedAtUtcTicks = 0; // as written by an older CLI

		Assert.That(Check(legacy, "tools/auth/"), Is.EqualTo(LocalStackLiveness.Liveness.Stopped),
			$"'{current}' is not a runtime the stack launches, so a legacy entry must not trust the pid");
		Assert.That(IsRunning(legacy), Is.False);
	}
}
