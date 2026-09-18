using Beamable.Common.Dependencies;
using Beamable.Common.BeamCli;
using Beamable.Server;
using cli;
using cli.CliServerCommand;
using cli.Services;
using cli.Services.HttpServer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tests;

// The server's invocation registry is static. Keep this fixture isolated from other CLI tests.
[NonParallelizable]
public class ServerServiceTests
{
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
	private static readonly ServeCliCommandArgs Args = new() { owner = "server-tests", useCustomSplitter = true };
	private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
	private static readonly Func<ServeCliCommandArgs, ulong, Task<ServerInfoResponse>> HandleInfo =
		typeof(ServerService).GetMethod("HandleInfo", PrivateStatic)!
			.CreateDelegate<Func<ServeCliCommandArgs, ulong, Task<ServerInfoResponse>>>();
	private static readonly Func<ServeCliCommandArgs, HttpListenerContext, ulong, App?, Task> HandleRequest =
		typeof(ServerService).GetMethod("HandleRequest", PrivateStatic)!
			.CreateDelegate<Func<ServeCliCommandArgs, HttpListenerContext, ulong, App?, Task>>();

	private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static Task<ServerInfoResponse> Snapshot() => HandleInfo(Args, 0);

	[TestCase("{broken")]
	[TestCase("null")]
	[TestCase("{}")]
	public async Task Invalid_execute_request_returns_a_framed_error(string body)
	{
		await using var server = new TestServer(null);
		using var request = new StringContent(body, Encoding.UTF8, "application/json");
		using var response = await server.Client.PostAsync("execute", request);
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Command failures are reported in the stream.");
		Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
		AssertErrorReport(await response.Content.ReadAsStringAsync());
		Assert.That((await Snapshot()).inflightCommands, Is.Empty);
		server.AssertNoErrors();
	}

	private static ErrorOutput AssertErrorReport(string response)
	{
		var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		Assert.That(lines, Has.Length.EqualTo(1), response);
		Assert.That(lines[0], Does.StartWith("data: "));
		var report = JsonConvert.DeserializeObject<ReportDataPoint<ErrorOutput>>(lines[0]["data: ".Length..])!;
		Assert.That(report.type, Does.StartWith("error"), "Unity dispatches OnError using the report channel.");
		Assert.That(report.ts, Is.GreaterThan(0));
		Assert.That(report.data.exitCode, Is.Not.EqualTo(0));
		Assert.That(report.data.message, Is.Not.Empty);
		Assert.That(report.data.typeName, Is.Not.Empty);
		return report.data;
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Typed_errors_are_preserved_without_duplicates(bool unhandled)
	{
		await using var server = new TestServer(() => new ControlledApp((command, reporter) =>
		{
			var failure = new CliException<FramingTestError>("expected failure", 7)
			{
				payload = new FramingTestError { detail = "preserved" }
			};
			if (unhandled)
			{
				throw failure;
			}
			reporter.Exception(failure, 7, command);
			return Task.FromResult(7);
		}));
		var response = await server.Execute("reported-error");
		var error = AssertErrorReport(response);
		Assert.That(error.exitCode, Is.EqualTo(7));
		var report = JObject.Parse(response["data: ".Length..]);
		Assert.That((string?)report["type"], Is.EqualTo("errorFramingTestError"));
		Assert.That((string?)report["data"]?["detail"], Is.EqualTo("preserved"));
		server.AssertNoErrors();
	}

	[Test]
	public async Task Partial_output_is_followed_by_a_framed_execution_error()
	{
		await using var server = new TestServer(() => new ControlledApp((_, reporter) =>
		{
			reporter.Report("stream", new ServerRequest { commandLine = "partial" });
			throw new InvalidOperationException("failed after partial output");
		}));
		var response = await server.Execute("partial-failure");
		var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		Assert.That(lines, Has.Length.EqualTo(2), response);
		Assert.That((string?)JObject.Parse(lines[0]["data: ".Length..])["type"], Is.EqualTo("stream"));
		Assert.That(AssertErrorReport(lines[1]).message, Is.EqualTo("failed after partial output"));
		Assert.That((await Snapshot()).inflightCommands, Is.Empty);
		server.AssertNoErrors();
	}

	[Test]
	public async Task Cleanup_failure_after_success_does_not_append_a_command_error()
	{
		var logger = new CompletionFailureLogger();
		await using var server = new TestServer(() => new ControlledApp((_, reporter) =>
		{
			reporter.Report("stream", new ServerRequest { commandLine = "success" });
			return Task.FromResult(0);
		}), logger);
		var response = await server.Execute("cleanup-failure");
		Assert.That(logger.FaultInjected, Is.True, "The cleanup failure must actually occur.");
		var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		Assert.That(lines, Has.Length.EqualTo(1), response);
		Assert.That(lines[0], Does.StartWith("data: "));
		var report = JObject.Parse(lines[0]["data: ".Length..]);
		Assert.That((string?)report["type"], Is.EqualTo("stream"));
		Assert.That((string?)report["data"]?["commandLine"], Is.EqualTo("success"));
		Assert.That(logger.DiagnosedFailure, Is.True, "Cleanup failures must remain visible in server diagnostics.");
		Assert.That((await Snapshot()).inflightCommands, Is.Empty);
		server.AssertNoErrors();
	}

	public sealed class FramingTestError : ErrorOutput
	{
		public string detail = "";
	}

	private sealed class CompletionFailureLogger : ILogger
	{
		public bool FaultInjected { get; private set; }
		public bool DiagnosedFailure { get; private set; }
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			// Fault the diagnostic emitted in HandleExec's finally, after command success and removal.
			// This exercises the real outer HTTP handler without corrupting the static registry.
			if (!FaultInjected && formatter(state, exception).StartsWith("CLI EXEC FINISHED WITH EXIT="))
			{
				FaultInjected = true;
				throw new InvalidOperationException("injected cleanup failure");
			}
			if (exception?.Message == "injected cleanup failure" && logLevel == LogLevel.Error)
			{
				DiagnosedFailure = true;
			}
		}
	}

	[TearDown]
	public void Check_and_reset_registry()
	{
		// Request workers have finished before fixture teardown. Reset even on failure so a leak
		// does not contaminate later tests (including runs against the original buggy implementation).
		var registry = (List<string>)typeof(ServerService)
			.GetField("cliInvocations", PrivateStatic | BindingFlags.Public)!.GetValue(null)!;
		try
		{
			Assert.That(registry, Is.Empty, "All completed requests must have been removed.");
		}
		finally
		{
			registry.Clear();
		}
	}

	[Test]
	public async Task Info_snapshot_survives_command_completion()
	{
		var entered = Signal();
		var release = Signal();
		await using var server = new TestServer(() => new ControlledApp(async (_, _) =>
		{
			entered.TrySetResult(true);
			await release.Task;
			return 0;
		}));
		var request = server.Execute("snapshot");
		try
		{
			await server.WaitForCommands(entered.Task, request);
			var snapshot = await Snapshot();
			Assert.That(snapshot.inflightCommands, Has.Count.EqualTo(1));
			release.TrySetResult(true);
			await request;
			Assert.That((await Snapshot()).inflightCommands, Is.Empty);
			Assert.That(snapshot.inflightCommands, Has.Count.EqualTo(1), "An earlier /info result must not alias the live registry.");
			server.AssertNoErrors();
		}
		finally
		{
			release.TrySetResult(true);
			await request;
		}
	}

	[Test]
	public async Task Identical_commands_remain_tracked_until_each_finishes()
	{
		var entered = Signal();
		var first = Signal();
		var second = Signal();
		var count = 0;
		await using var server = new TestServer(() => new ControlledApp(async (_, _) =>
		{
			var index = Interlocked.Increment(ref count);
			if (index == 2)
			{
				entered.TrySetResult(true);
			}
			await (index == 1 ? first.Task : second.Task);
			return 0;
		}));
		var requests = new[] { server.Execute("same"), server.Execute("same") };
		try
		{
			await server.WaitForCommands(entered.Task, requests);
			Assert.That((await Snapshot()).inflightCommands, Has.Count.EqualTo(2));
			first.TrySetResult(true);
			await await Task.WhenAny(requests);
			Assert.That((await Snapshot()).inflightCommands, Has.Count.EqualTo(1));
			second.TrySetResult(true);
			await Task.WhenAll(requests);
			Assert.That((await Snapshot()).inflightCommands, Is.Empty);
			server.AssertNoErrors();
		}
		finally
		{
			first.TrySetResult(true);
			second.TrySetResult(true);
			await Task.WhenAll(requests);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Concurrent_execution_and_info_reads_preserve_results_and_clear_registry(bool identicalCommands)
	{
		const int requestCount = 32;
		for (var round = 0; round < 8; round++)
		{
			var entered = Signal();
			var release = Signal();
			var count = 0;
			await using var server = new TestServer(() => new ControlledApp(async (command, reporter) =>
			{
				if (Interlocked.Increment(ref count) == requestCount)
				{
					entered.TrySetResult(true);
				}
				await release.Task;
				reporter.Report("stream", new ServerRequest { commandLine = command });
				return 0;
			}));
			var commands = Enumerable.Range(0, requestCount).Select(i => identicalCommands ? "same" : $"command-{i}").ToArray();
			var requests = commands.Select(server.Execute).ToArray();
			Task reads = Task.CompletedTask;
			try
			{
				await server.WaitForCommands(entered.Task, requests);
				var snapshot = await Snapshot();
				Assert.That(snapshot.inflightCommands, Has.Count.EqualTo(requestCount));
				reads = Task.Run(async () =>
				{
					for (var i = 0; i < 50; i++)
					{
						var info = JObject.Parse(await server.Client.GetStringAsync("info"));
						Assert.That(info["inflightCommands"], Is.TypeOf<JArray>());
					}
				});
				release.TrySetResult(true);
				var responses = await Task.WhenAll(requests);
				await reads;
				for (var i = 0; i < responses.Length; i++)
				{
					var lines = responses[i].Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					Assert.That(lines, Has.Length.EqualTo(1), responses[i]);
					Assert.That(lines[0], Does.StartWith("data: "));
					var report = JObject.Parse(lines[0]["data: ".Length..]);
					Assert.That((string?)report["type"], Is.EqualTo("stream"));
					Assert.That((string?)report["data"]?["commandLine"], Is.EqualTo(commands[i]));
				}
				Assert.That((await Snapshot()).inflightCommands, Is.Empty);
				Assert.That(snapshot.inflightCommands, Has.Count.EqualTo(requestCount));
				server.AssertNoErrors();
			}
			finally
			{
				release.TrySetResult(true);
				await Task.WhenAll(requests);
				await reads;
			}
		}
	}

	[Test]
	public async Task Setup_failure_does_not_register_a_command()
	{
		var failure = new InvalidOperationException("injected setup failure\nsecond line — preserved");
		var executed = false;
		await using var server = new TestServer(() => new ControlledApp((_, _) =>
		{
			executed = true;
			return Task.FromResult(0);
		}) { SetupFailure = failure });
		var error = AssertErrorReport(await server.Execute("setup-failure"));
		Assert.That(error.message, Is.EqualTo(failure.Message));
		Assert.That(error.invocation, Is.EqualTo("setup-failure"));
		server.AssertNoErrors();
		Assert.That(executed, Is.False);
		Assert.That((await Snapshot()).inflightCommands, Is.Empty);
	}

	[Test]
	public async Task Execution_failure_removes_the_registered_command()
	{
		var observedCount = 0;
		await using var server = new TestServer(() => new ControlledApp(async (_, _) =>
		{
			observedCount = (await Snapshot()).inflightCommands.Count;
			throw new InvalidOperationException("injected execution failure");
		}));
		var error = AssertErrorReport(await server.Execute("execution-failure"));
		Assert.That(error.message, Is.EqualTo("injected execution failure"));
		Assert.That(error.invocation, Is.EqualTo("execution-failure"));
		Assert.That(observedCount, Is.EqualTo(1));
		Assert.That((await Snapshot()).inflightCommands, Is.Empty);
		server.AssertNoErrors();
	}

	// Keep the real handler, DI registration and ServerReporterService. Only command work is controlled.
	private sealed class ControlledApp : App, IDisposable
	{
		private readonly Func<string, IDataReporterService, Task<int>> _execute;
		private readonly AppLifecycle _lifecycle = new();
		public Exception? SetupFailure { get; init; }
		public ControlledApp(Func<string, IDataReporterService, Task<int>> execute) => _execute = execute;

		public override void Configure(Action<IDependencyBuilder>? serviceConfigurator = null,
			Action<IDependencyBuilder>? commandConfigurator = null, Action<ILoggingBuilder>? configureLogger = null,
			bool overwriteLogger = true)
		{
			// These tests exercise result reporting, not the unrelated background heartbeat.
			_lifecycle.Cancel();
			Commands.AddSingleton(_lifecycle);
			Commands.AddSingleton<IDataReporterService>(_ =>
				throw new InvalidOperationException("The handler must replace the default reporter."));
			serviceConfigurator!(Commands);
		}

		public override void Build()
		{
			if (SetupFailure != null)
			{
				throw SetupFailure;
			}
			CommandProvider = Commands.Build();
		}

		public override Task<int> RunWithSingleString(string commandLine, bool useCustomSplitter) =>
			_execute(commandLine, CommandProvider.GetService<IDataReporterService>());

		public void Dispose()
		{
			CommandProvider?.Dispose();
			_lifecycle.Source.Dispose();
		}
	}

	private sealed class TestServer : IAsyncDisposable
	{
		private readonly HttpListener _listener = new();
		private readonly Func<ControlledApp>? _createApp;
		private readonly ILogger _logger;
		private readonly List<Task> _handlers = new();
		private readonly Task _accept;
		private readonly TaskCompletionSource<bool> _failed = Signal();
		private volatile bool _stopping;
		public readonly ConcurrentQueue<Exception> Errors = new();
		public HttpClient Client { get; }

		public TestServer(Func<ControlledApp>? createApp, ILogger? logger = null)
		{
			_createApp = createApp;
			_logger = logger ?? NullLogger.Instance;
			// Register MSBuild once before request workers construct their Apps concurrently.
			_ = new App();
			using var portReservation = new TcpListener(IPAddress.Loopback, 0);
			portReservation.Start();
			var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
			portReservation.Stop();
			var uri = new Uri($"http://127.0.0.1:{port}/");
			_listener.Prefixes.Add(uri.ToString());
			_listener.Start();
			Client = new HttpClient(new SocketsHttpHandler { UseProxy = false, MaxConnectionsPerServer = 64 })
			{
				BaseAddress = uri,
				Timeout = Deadline
			};
			_accept = Accept();
		}

		public async Task<string> Execute(string command)
		{
			using var body = new StringContent(JsonConvert.SerializeObject(new ServerRequest { commandLine = command }),
				Encoding.UTF8, "application/json");
			using var response = await Client.PostAsync("execute", body);
			return await response.Content.ReadAsStringAsync();
		}

		public async Task WaitForCommands(Task entered, params Task[] requests)
		{
			await Task.WhenAny(entered, Task.WhenAll(requests), _failed.Task).WaitAsync(Deadline);
			AssertNoErrors();
			Assert.That(entered.IsCompletedSuccessfully, Is.True, "Requests finished before command execution reached its gate.");
		}

		private async Task Accept()
		{
			try
			{
				while (_listener.IsListening)
				{
					var context = await _listener.GetContextAsync();
					_handlers.Add(Task.Run(() => Handle(context)));
				}
			}
			catch (HttpListenerException) when (_stopping) { }
			catch (ObjectDisposedException) when (_stopping) { }
		}

		private async Task Handle(HttpListenerContext context)
		{
			// Each request has its own async logging context, just as the CLI normally provides.
			BeamableZLoggerProvider.LogContext.Value = _logger;
			try
			{
				using var app = context.Request.Url!.AbsolutePath == "/execute" ? _createApp?.Invoke() : null;
				await HandleRequest(Args, context, 0, app);
			}
			catch (Exception ex)
			{
				Errors.Enqueue(ex);
				_failed.TrySetResult(true);
			}
			finally
			{
				context.Response.Close();
			}
		}

		public void AssertNoErrors() => Assert.That(Errors, Is.Empty, string.Join(Environment.NewLine, Errors));

		public async ValueTask DisposeAsync()
		{
			_stopping = true;
			_listener.Stop();
			await _accept.WaitAsync(Deadline);
			await Task.WhenAll(_handlers).WaitAsync(Deadline);
			_listener.Close();
			Client.Dispose();
		}
	}
}
