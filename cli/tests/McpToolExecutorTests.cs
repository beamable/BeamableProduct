using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Server;
using cli;
using cli.Commands.Mcp;
using cli.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace tests;

[NonParallelizable]
public class McpToolExecutorTests
{
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);

	/// <summary>Keeps the real reporter wiring and cancellation; only the command's work is scripted.</summary>
	internal sealed class ScriptedApp : App
	{
		private readonly Func<string, IDataReporterService, CancellationToken, Task<int>> _run;
		private readonly AppLifecycle _lifecycle = new();

		public ScriptedApp(Func<string, IDataReporterService, CancellationToken, Task<int>> run) => _run = run;

		public override void Configure(Action<IDependencyBuilder>? serviceConfigurator = null,
			Action<IDependencyBuilder>? commandConfigurator = null, Action<ILoggingBuilder>? configureLogger = null,
			bool overwriteLogger = true)
		{
			Commands.AddSingleton(_lifecycle);
			Commands.AddSingleton<IDataReporterService>(_ =>
				throw new InvalidOperationException("The executor must replace the default reporter."));
			serviceConfigurator!(Commands);
		}

		public override void Build() => CommandProvider = Commands.Build();

		public override Task<int> RunWithSingleString(string commandLine, bool useCustomSplitter) =>
			// Like a real command (args.Lifecycle), resolve the lifecycle from the command's services, so the
			// executor's cancellation wiring is what reaches it.
			_run(commandLine, CommandProvider.GetService<IDataReporterService>(), CommandProvider.GetService<AppLifecycle>().CancellationToken);
	}

	// Report payloads are serialized by public fields, like the CLI's own result types.
	public class Named { public string name = ""; }
	public class Ratio { public float ratio; }
	public class Message { public string message = ""; }
	public class Done { public bool done; }

	private static McpToolExecutor Executor(Func<string, IDataReporterService, CancellationToken, Task<int>> run) =>
		new() { AppFactory = () => new ScriptedApp(run) };

	private static string Text(CallToolResult result) => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

	private sealed class Progress : IProgress<ProgressNotificationValue>
	{
		public readonly ConcurrentQueue<ProgressNotificationValue> Values = new();
		public void Report(ProgressNotificationValue value) => Values.Enqueue(value);
	}

	[Test]
	public async Task Results_are_returned_as_text_and_structured_content()
	{
		var executor = Executor((_, reporter, _) =>
		{
			reporter.Report("stream", new Named { name = "first" });
			reporter.Report("stream", new Named { name = "second" });
			return Task.FromResult(0);
		});
		var result = await executor.ExecuteCommandAsync("services ps");

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(Text(result), Does.Contain("\"first\"").And.Contain("\"second\""));
		var structured = result.StructuredContent!.Value;
		Assert.That(structured.GetProperty("exitCode").GetInt32(), Is.EqualTo(0));
		var events = structured.GetProperty("events").EnumerateArray().ToList();
		Assert.That(events.Select(e => e.GetProperty("data").GetProperty("name").GetString()), Is.EqualTo(new[] { "first", "second" }));
	}

	[Test]
	public async Task Commands_get_raw_output_and_log_streams()
	{
		string? received = null;
		var executor = Executor((command, _, _) =>
		{
			received = command;
			return Task.FromResult(0);
		});
		await executor.ExecuteCommandAsync("config");
		Assert.That(received, Does.Contain("--raw").And.Contain("--emit-log-streams"));
	}

	[Test]
	public async Task Non_zero_exit_is_an_error_result()
	{
		var executor = Executor((command, reporter, _) =>
		{
			reporter.Exception(new CliException("it broke", 3, true), 3, command);
			return Task.FromResult(3);
		});
		var result = await executor.ExecuteCommandAsync("content publish");
		Assert.That(result.IsError, Is.True);
		Assert.That(result.StructuredContent!.Value.GetProperty("exitCode").GetInt32(), Is.EqualTo(3));
		Assert.That(Text(result), Does.Contain("it broke"));
	}

	[Test]
	public async Task Thrown_failure_is_an_error_result()
	{
		var executor = Executor((_, _, _) => throw new InvalidOperationException("setup exploded"));
		var result = await executor.ExecuteCommandAsync("deploy release");
		Assert.That(result.IsError, Is.True);
		Assert.That(Text(result), Does.Contain("setup exploded"));
	}

	[Test]
	public async Task Unknown_command_errors_carry_the_retry_hint()
	{
		var executor = Executor((command, reporter, _) =>
		{
			reporter.Exception(new CliException("Unrecognized command or argument 'nope'."), 1, command);
			return Task.FromResult(1);
		});
		var result = await executor.ExecuteCommandAsync("nope");
		Assert.That(Text(result), Does.Contain("beam_list_commands"));
	}

	[Test]
	public async Task Cancellation_stops_the_command()
	{
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var observedCancel = false;
		var executor = Executor(async (_, _, lifecycle) =>
		{
			started.SetResult();
			try
			{
				await Task.Delay(Timeout.Infinite, lifecycle);
			}
			catch (OperationCanceledException)
			{
				observedCancel = true;
			}
			return 0;
		});
		using var cts = new CancellationTokenSource();
		var call = executor.ExecuteCommandAsync("project run", cancellationToken: cts.Token);
		await started.Task.WaitAsync(Deadline);
		cts.Cancel();
		var result = await call.WaitAsync(Deadline);

		Assert.That(observedCancel, Is.True, "The command's AppLifecycle must be cancelled.");
		Assert.That(result.IsError, Is.True);
		Assert.That(Text(result), Does.Contain("cancelled"));
	}

	[Test]
	public async Task Concurrent_calls_do_not_wait_for_each_other()
	{
		const int count = 4;
		var running = 0;
		var allRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var executor = Executor(async (_, _, _) =>
		{
			if (Interlocked.Increment(ref running) == count)
			{
				allRunning.SetResult();
			}
			// Each call only finishes once all of them are in flight at the same time.
			await allRunning.Task;
			return 0;
		});
		var calls = Enumerable.Range(0, count).Select(i => executor.ExecuteCommandAsync($"cmd-{i}")).ToArray();
		var results = await Task.WhenAll(calls).WaitAsync(Deadline);
		Assert.That(results.All(r => r.IsError != true), Is.True);
	}

	[Test]
	public async Task Progress_channels_become_progress_notifications()
	{
		var executor = Executor((_, reporter, _) =>
		{
			reporter.Report("progressStream", new Ratio { ratio = 0.25f });
			reporter.Report("progressStream", new Ratio { ratio = 0.75f });
			reporter.Report("remote_progress", new Message { message = "no ratio" });
			reporter.Report("stream", new Done { done = true });
			return Task.FromResult(0);
		});
		var progress = new Progress();
		var result = await executor.ExecuteCommandAsync("content publish", progress);

		var values = progress.Values.ToList();
		Assert.That(values, Has.Count.EqualTo(3));
		Assert.That(values.Select(v => v.Progress).ToList(), Is.Ordered.Ascending, "MCP progress must increase.");
		Assert.That(values[0].Progress, Is.EqualTo(0.25f));
		Assert.That(values[0].Total, Is.EqualTo(1f));
		Assert.That(Text(result), Does.Not.Contain("progressStream"), "Progress is transient, not part of the result.");
		Assert.That(Text(result), Does.Contain("\"done\""));
	}

	[Test]
	public async Task Only_warning_and_error_logs_are_kept()
	{
		var executor = Executor((_, reporter, _) =>
		{
			reporter.Report("logs", CliLogMessage.FromStringNow("chatty detail", "Debug"));
			reporter.Report("logs", CliLogMessage.FromStringNow("disk almost full", "Warning"));
			return Task.FromResult(0);
		});
		var text = Text(await executor.ExecuteCommandAsync("config"));
		Assert.That(text, Does.Contain("disk almost full"));
		Assert.That(text, Does.Not.Contain("chatty detail"));
	}

	[Test]
	public async Task Running_commands_are_listed_by_the_server_registry()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var executor = Executor(async (_, _, _) =>
		{
			entered.SetResult();
			await release.Task;
			return 0;
		});
		var call = executor.ExecuteCommandAsync("services ps");
		await entered.Task.WaitAsync(Deadline);
		Assert.That(cli.Services.CliInvocation.CliInvocationCore.Snapshot(), Has.One.Contains("services ps"));
		release.SetResult();
		await call.WaitAsync(Deadline);
		Assert.That(cli.Services.CliInvocation.CliInvocationCore.Snapshot(), Has.None.Contains("services ps"));
	}

	public class Steps { public List<string> steps = new(); }

	[Test]
	public async Task Console_output_is_part_of_the_result()
	{
		// Like `beam local logs`: the log lines go to the console, the result only names the steps.
		var executor = Executor((_, reporter, _) =>
		{
			Console.Out.WriteLine("[demo] line-a");
			Console.WriteLine("[demo] line-b");
			reporter.Report("stream", new Steps { steps = { "demo" } });
			return Task.FromResult(0);
		});
		var result = await executor.ExecuteCommandAsync("local logs");

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(Text(result), Does.Contain("[demo] line-a").And.Contain("[demo] line-b").And.Contain("\"steps\""));
		Assert.That(result.StructuredContent!.Value.GetProperty("console").GetString(), Does.Contain("[demo] line-a"));
	}

	[Test]
	public async Task Concurrent_calls_capture_only_their_own_console_output()
	{
		const int count = 6;
		var barrier = new Barrier(count);
		var executor = Executor((command, _, _) =>
		{
			var marker = command.Split(' ')[0];
			for (var i = 0; i < 20; i++)
			{
				Console.Out.WriteLine($"{marker}-{i}");
				if (i == 5)
				{
					barrier.SignalAndWait(TimeSpan.FromSeconds(10)); // all calls write at the same time
				}
			}
			return Task.FromResult(0);
		});
		var calls = Enumerable.Range(0, count).Select(i => Task.Run(() => executor.ExecuteCommandAsync($"marker{i}"))).ToArray();
		var results = await Task.WhenAll(calls).WaitAsync(Deadline);

		for (var i = 0; i < count; i++)
		{
			var text = Text(results[i]);
			Assert.That(text, Does.Contain($"marker{i}-19"));
			var foreign = Enumerable.Range(0, count).Where(j => j != i).Where(j => text.Contains($"marker{j}-")).ToList();
			Assert.That(foreign, Is.Empty, $"Call {i} captured another call's output.");
		}
	}

	[Test]
	public async Task Writes_outside_a_call_are_not_captured_by_it()
	{
		// The host and Unity's /execute commands share the process; their console writes must not end up in an
		// agent's tool result while a call is running.
		var inCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var executor = Executor(async (_, _, _) =>
		{
			Console.Out.WriteLine("from-the-call");
			inCall.SetResult();
			await release.Task;
			return 0;
		});
		var call = executor.ExecuteCommandAsync("config");
		await inCall.Task.WaitAsync(Deadline);

		// This test's own flow is outside the call, like the host's.
		Console.Out.WriteLine("from-the-host");
		await Task.Run(() => Console.Out.WriteLine("from-the-host-task"));
		release.SetResult();
		var text = Text(await call.WaitAsync(Deadline));

		Assert.That(text, Does.Contain("from-the-call"));
		Assert.That(text, Does.Not.Contain("from-the-host"));
		Assert.That(text, Does.Not.Contain("from-the-host-task"));
	}
}
